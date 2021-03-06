﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class DefinitionMap
    {
        protected struct MethodDefinitionEntry
        {
            public MethodDefinitionEntry(IMethodSymbolInternal previousMethod, bool preserveLocalVariables, Func<SyntaxNode, SyntaxNode> syntaxMap)
            {
                this.PreviousMethod = previousMethod;
                this.PreserveLocalVariables = preserveLocalVariables;
                this.SyntaxMap = syntaxMap;
            }

            public readonly IMethodSymbolInternal PreviousMethod;
            public readonly bool PreserveLocalVariables;
            public readonly Func<SyntaxNode, SyntaxNode> SyntaxMap;
        }

        protected readonly PEModule module;
        protected readonly IReadOnlyDictionary<IMethodSymbol, MethodDefinitionEntry> methodMap;

        protected DefinitionMap(PEModule module, IEnumerable<SemanticEdit> edits)
        {
            Debug.Assert(module != null);
            Debug.Assert(edits != null);

            this.module = module;
            this.methodMap = GenerateMethodMap(edits);
        }

        private static IReadOnlyDictionary<IMethodSymbol, MethodDefinitionEntry> GenerateMethodMap(IEnumerable<SemanticEdit> edits)
        {
            var methodMap = new Dictionary<IMethodSymbol, MethodDefinitionEntry>();
            foreach (var edit in edits)
            {
                if (edit.Kind == SemanticEditKind.Update)
                {
                    var method = edit.NewSymbol as IMethodSymbol;
                    if (method != null)
                    {
                        methodMap.Add(method, new MethodDefinitionEntry(
                            (IMethodSymbolInternal)edit.OldSymbol,
                            edit.PreserveLocalVariables,
                            edit.SyntaxMap));
                    }
                }
            }

            return methodMap;
        }

        internal abstract Cci.IDefinition MapDefinition(Cci.IDefinition definition);
        internal abstract Cci.ITypeReference MapReference(Cci.ITypeReference reference);

        internal bool DefinitionExists(Cci.IDefinition definition)
        {
            return MapDefinition(definition) != null;
        }

        internal abstract bool TryGetTypeHandle(Cci.ITypeDefinition def, out TypeDefinitionHandle handle);
        internal abstract bool TryGetEventHandle(Cci.IEventDefinition def, out EventDefinitionHandle handle);
        internal abstract bool TryGetFieldHandle(Cci.IFieldDefinition def, out FieldDefinitionHandle handle);
        internal abstract bool TryGetMethodHandle(Cci.IMethodDefinition def, out MethodDefinitionHandle handle);
        internal abstract bool TryGetPropertyHandle(Cci.IPropertyDefinition def, out PropertyDefinitionHandle handle);

    }

    internal abstract class DefinitionMap<TSymbolMatcher> : DefinitionMap
        where TSymbolMatcher : SymbolMatcher
    {
        protected readonly TSymbolMatcher mapToMetadata;
        protected readonly TSymbolMatcher mapToPrevious;
        
        protected DefinitionMap(PEModule module, IEnumerable<SemanticEdit> edits, TSymbolMatcher mapToMetadata, TSymbolMatcher mapToPrevious)
            : base(module, edits)
        {
            Debug.Assert(mapToMetadata != null);

            this.mapToMetadata = mapToMetadata;
            this.mapToPrevious = mapToPrevious ?? mapToMetadata;
        }

        internal sealed override Cci.IDefinition MapDefinition(Cci.IDefinition definition)
        {
            return this.mapToPrevious.MapDefinition(definition) ??
                   (this.mapToMetadata != this.mapToPrevious ? this.mapToMetadata.MapDefinition(definition) : null);
        }

        internal sealed override Cci.ITypeReference MapReference(Cci.ITypeReference reference)
        {
            return this.mapToPrevious.MapReference(reference) ??
                   (this.mapToMetadata != this.mapToPrevious ? this.mapToMetadata.MapReference(reference) : null);
        }

        private bool TryGetMethodHandle(EmitBaseline baseline, Cci.IMethodDefinition def, out MethodDefinitionHandle handle)
        {
            if (this.TryGetMethodHandle(def, out handle))
            {
                return true;
            }

            def = (Cci.IMethodDefinition)this.mapToPrevious.MapDefinition(def);
            if (def != null)
            {
                uint methodIndex;
                if (baseline.MethodsAdded.TryGetValue(def, out methodIndex))
                {
                    handle = MetadataTokens.MethodDefinitionHandle((int)methodIndex);
                    return true;
                }
            }

            handle = default(MethodDefinitionHandle);
            return false;
        }

        protected static IReadOnlyDictionary<SyntaxNode, int> CreateDeclaratorToSyntaxOrdinalMap(ImmutableArray<SyntaxNode> declarators)
        {
            var declaratorToIndex = new Dictionary<SyntaxNode, int>();
            for (int i = 0; i < declarators.Length; i++)
            {
                declaratorToIndex.Add(declarators[i], i);
            }

            return declaratorToIndex;
        }

        protected abstract void GetStateMachineFieldMapFromMetadata(
            ITypeSymbol stateMachineType,
            ImmutableArray<LocalSlotDebugInfo> localSlotDebugInfo,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap,
            out int awaiterSlotCount);

        internal VariableSlotAllocator TryCreateVariableSlotAllocator(EmitBaseline baseline, IMethodSymbol method)
        {
            MethodDefinitionHandle handle;
            if (!this.TryGetMethodHandle(baseline, (Cci.IMethodDefinition)method, out handle))
            {
                // Unrecognized method. Must have been added in the current compilation.
                return null;
            }

            MethodDefinitionEntry methodEntry;
            if (!this.methodMap.TryGetValue(method, out methodEntry))
            {
                // Not part of changeset. No need to preserve locals.
                return null;
            }

            if (!methodEntry.PreserveLocalVariables)
            {
                // We should always "preserve locals" of iterator and async methods since the state machine 
                // might be active without MoveNext method being on stack. We don't enforce this requirement here,
                // since a method may be incorrectly marked by Iterator/AsyncStateMachine attribute by the user, 
                // in which case we can't reliably figure out that it's an error in semantic edit set. 

                return null;
            }

            ImmutableArray<EncLocalInfo> previousLocals;
            IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap = null;
            IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap = null;
            int hoistedLocalSlotCount = 0;
            int awaiterSlotCount = 0;
            string stateMachineTypeNameOpt = null;
            TSymbolMatcher symbolMap;

            uint methodIndex = (uint)MetadataTokens.GetRowNumber(handle);
             
            // Check if method has changed previously. If so, we already have a map.
            AddedOrChangedMethodInfo addedOrChangedMethod;
            if (baseline.AddedOrChangedMethods.TryGetValue(methodIndex, out addedOrChangedMethod))
            {
                if (addedOrChangedMethod.StateMachineTypeNameOpt != null)
                {
                    // method is async/iterator kickoff method
                    GetStateMachineFieldMapFromPreviousCompilation(
                        addedOrChangedMethod.StateMachineHoistedLocalSlotsOpt,
                        addedOrChangedMethod.StateMachineAwaiterSlotsOpt,
                        out hoistedLocalMap,
                        out awaiterMap);

                    hoistedLocalSlotCount = addedOrChangedMethod.StateMachineHoistedLocalSlotsOpt.Length;
                    awaiterSlotCount = addedOrChangedMethod.StateMachineAwaiterSlotsOpt.Length;

                    // Kickoff method has no interesting locals on its own. 
                    // We use the EnC method debug infromation for hoisted locals.
                    previousLocals = ImmutableArray<EncLocalInfo>.Empty;

                    stateMachineTypeNameOpt = addedOrChangedMethod.StateMachineTypeNameOpt;
                }
                else
                {
                    previousLocals = addedOrChangedMethod.Locals;
                }

                // All types that AddedOrChangedMethodInfo refers to have been mapped to the previous generation.
                // Therefore we don't need to fall back to metadata if we don't find the type reference, like we do in DefinitionMap.MapReference.
                symbolMap = mapToPrevious;
            }
            else
            {
                // Method has not changed since initial generation. Generate a map
                // using the local names provided with the initial metadata.
                var debugInfo = baseline.DebugInformationProvider(handle);
                ITypeSymbol stateMachineType = TryGetStateMachineType(handle);
                if (stateMachineType != null)
                {
                    // method is async/iterator kickoff method
                    var localSlotDebugInfo = debugInfo.LocalSlots.NullToEmpty();
                    GetStateMachineFieldMapFromMetadata(stateMachineType, localSlotDebugInfo, out hoistedLocalMap, out awaiterMap, out awaiterSlotCount);
                    hoistedLocalSlotCount = localSlotDebugInfo.Length;

                    // Kickoff method has no interesting locals on its own. 
                    // We use the EnC method debug infromation for hoisted locals.
                    previousLocals = ImmutableArray<EncLocalInfo>.Empty;

                    stateMachineTypeNameOpt = stateMachineType.Name;
                }
                else
                {
                    previousLocals = TryGetLocalSlotMapFromMetadata(handle, debugInfo);
                    if (previousLocals.IsDefault)
                    {
                        // TODO: Report error that metadata is not supported.
                        return null;
                    }
                }

                symbolMap = mapToMetadata;
            }

            return new EncVariableSlotAllocator(
                symbolMap,
                methodEntry.SyntaxMap,
                methodEntry.PreviousMethod,
                previousLocals,
                stateMachineTypeNameOpt,
                hoistedLocalSlotCount,
                hoistedLocalMap,
                awaiterSlotCount,
                awaiterMap);
        }

        protected abstract ImmutableArray<EncLocalInfo> TryGetLocalSlotMapFromMetadata(MethodDefinitionHandle handle, EditAndContinueMethodDebugInformation debugInfo);
        protected abstract ITypeSymbol TryGetStateMachineType(Handle methodHandle);

        private static void GetStateMachineFieldMapFromPreviousCompilation(
            ImmutableArray<EncHoistedLocalInfo> hoistedLocalSlots,
            ImmutableArray<Cci.ITypeReference> hoistedAwaiters,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap)
        {
            var hoistedLocals = new Dictionary<EncHoistedLocalInfo, int>();
            var awaiters = new Dictionary<Cci.ITypeReference, int>();

            for (int slotIndex = 0; slotIndex < hoistedLocalSlots.Length; slotIndex++)
            {
                var slot = hoistedLocalSlots[slotIndex];
                if (slot.IsUnused)
                {
                    // Unused field.
                    continue;
                }

                hoistedLocals.Add(slot, slotIndex);
            }

            for (int slotIndex = 0; slotIndex < hoistedAwaiters.Length; slotIndex++)
            {
                var slot = hoistedAwaiters[slotIndex];
                if (slot == null)
                {
                    // Unused awaiter.
                    continue;
                }

                awaiters.Add(slot, slotIndex);
            }

            hoistedLocalMap = hoistedLocals;
            awaiterMap = awaiters;
        }
    }
}
