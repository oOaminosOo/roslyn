﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AbstractLinkedFileMergeConflictCommentAdditionService : ILinkedFileMergeConflictCommentAdditionService
    {
        internal abstract string GetConflictCommentText(string header, string beforeString, string afterString);

        public IEnumerable<TextChange> CreateCommentsForUnmergedChanges(SourceText originalSourceText, IEnumerable<UnmergedDocumentChanges> unmergedChanges)
        {
            var commentChanges = new List<TextChange>();

            foreach (var documentWithChanges in unmergedChanges)
            {
                var partitionedChanges = PartitionChangesForDocument(documentWithChanges.UnmergedChanges, originalSourceText);
                var comments = GetCommentChangesForDocument(partitionedChanges, documentWithChanges.ProjectName, originalSourceText, documentWithChanges.Text);

                commentChanges.AddRange(comments);
            }

            return commentChanges;
        }

        private IEnumerable<IEnumerable<TextChange>> PartitionChangesForDocument(IEnumerable<TextChange> changes, SourceText originalSourceText)
        {
            var partitionedChanges = new List<IEnumerable<TextChange>>();
            var currentPartition = new List<TextChange>();

            currentPartition.Add(changes.First());
            var currentPartitionEndLine = originalSourceText.Lines.GetLineFromPosition(changes.First().Span.End);

            foreach (var change in changes.Skip(1))
            {
                // If changes are on adjacent lines, consider them part of the same change.
                var changeStartLine = originalSourceText.Lines.GetLineFromPosition(change.Span.Start);
                if (changeStartLine.LineNumber >= currentPartitionEndLine.LineNumber + 2)
                {
                    partitionedChanges.Add(currentPartition);
                    currentPartition = new List<TextChange>();
                }

                currentPartition.Add(change);
                currentPartitionEndLine = originalSourceText.Lines.GetLineFromPosition(change.Span.End);
            }

            if (currentPartition.Any())
            {
                partitionedChanges.Add(currentPartition);
            }

            return partitionedChanges;
        }

        private List<TextChange> GetCommentChangesForDocument(IEnumerable<IEnumerable<TextChange>> partitionedChanges, string projectName, SourceText oldDocumentText, SourceText newDocumentText)
        {
            var commentChanges = new List<TextChange>();

            foreach (var changePartition in partitionedChanges)
            {
                var startPosition = changePartition.First().Span.Start;
                var endPosition = changePartition.Last().Span.End;

                var startLineStartPosition = oldDocumentText.Lines.GetLineFromPosition(startPosition).Start;
                var endLineEndPosition = oldDocumentText.Lines.GetLineFromPosition(endPosition).End;

                var oldText = oldDocumentText.GetSubText(TextSpan.FromBounds(startLineStartPosition, endLineEndPosition));
                var adjustedChanges = changePartition.Select(c => new TextChange(TextSpan.FromBounds(c.Span.Start - startLineStartPosition, c.Span.End - startLineStartPosition), c.NewText));
                var newText = oldText.WithChanges(adjustedChanges);

                var warningText = GetConflictCommentText(
                    string.Format(WorkspacesResources.UnmergedChangeFromProject, projectName),
                    TrimBlankLines(oldText),
                    TrimBlankLines(newText));

                if (warningText != null)
                {
                    commentChanges.Add(new TextChange(TextSpan.FromBounds(startLineStartPosition, startLineStartPosition), warningText));
                }
            }

            return commentChanges;
        }

        private string TrimBlankLines(SourceText text)
        {
            int startLine, endLine;
            for (startLine = 0; startLine < text.Lines.Count; startLine++)
            {
                if (!text.Lines[startLine].IsEmptyOrWhitespace())
                {
                    break;
                }
            }

            for (endLine = text.Lines.Count - 1; endLine > startLine; endLine--)
            {
                if (!text.Lines[endLine].IsEmptyOrWhitespace())
                {
                    break;
                }
            }

            return startLine <= endLine
                ? text.GetSubText(TextSpan.FromBounds(text.Lines[startLine].Start, text.Lines[endLine].End)).ToString()
                : null;
        }
    }
}
