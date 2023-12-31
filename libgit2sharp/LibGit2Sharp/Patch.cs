using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the patch between two trees.
    /// <para>The individual patches for each file can be accessed through the indexer of this class.</para>
    /// <para>Building a patch is an expensive operation. If you only need to know which files have been added,
    /// deleted, modified, ..., then consider using a simpler <see cref="TreeChanges"/>.</para>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Patch : IEnumerable<PatchEntryChanges>, IDiffResult
    {
        private readonly StringBuilder fullPatchBuilder = new StringBuilder();

        private readonly IDictionary<FilePath, PatchEntryChanges> changes = new Dictionary<FilePath, PatchEntryChanges>();
        private int linesAdded;
        private int linesDeleted;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Patch()
        { }

        internal unsafe Patch(DiffHandle diff)
        {
            using (diff)
            {
                int count = Proxy.git_diff_num_deltas(diff);
                for (int i = 0; i < count; i++)
                {
                    using (var patch = Proxy.git_patch_from_diff(diff, i))
                    {
                        var delta = Proxy.git_diff_get_delta(diff, i);
                        AddFileChange(delta);
                        Proxy.git_patch_print(patch, PrintCallBack);
                    }
                }
            }
        }

        /// <summary>
        /// Instantiate a patch from its content.
        /// </summary>
        /// <param name="content">The patch content</param>
        /// <returns>The Patch instance</returns>
        public static Patch FromPatchContent(string content)
        {
            return new Patch(Proxy.git_diff_from_buffer(content, (UIntPtr)content.Length));
        }

        private unsafe void AddFileChange(git_diff_delta* delta)
        {
            var treeEntryChanges = new TreeEntryChanges(delta);

            changes.Add(treeEntryChanges.Path, new PatchEntryChanges(delta->flags.HasFlag(GitDiffFlags.GIT_DIFF_FLAG_BINARY), treeEntryChanges));
        }

        private unsafe int PrintCallBack(git_diff_delta* delta, GitDiffHunk hunk, GitDiffLine line, IntPtr payload)
        {
            string patchPart = LaxUtf8Marshaler.FromNative(line.content, (int)line.contentLen);

            // Deleted files mean no "new file" path

            var pathPtr = delta->new_file.Path != null
                ? delta->new_file.Path
                : delta->old_file.Path;
            var filePath = LaxFilePathMarshaler.FromNative(pathPtr);

            PatchEntryChanges currentChange = this[filePath];
            string prefix = string.Empty;

            switch (line.lineOrigin)
            {
                case GitDiffLineOrigin.GIT_DIFF_LINE_CONTEXT:
                    prefix = " ";
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_ADDITION:
                    linesAdded++;
                    currentChange.LinesAdded++;
                    currentChange.AddedLines.Add(new Line(line.NewLineNo, patchPart));
                    prefix = "+";
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_DELETION:
                    linesDeleted++;
                    currentChange.LinesDeleted++;
                    currentChange.DeletedLines.Add(new Line(line.OldLineNo, patchPart));
                    prefix = "-";
                    break;
            }

            string formattedOutput = string.Concat(prefix, patchPart);

            fullPatchBuilder.Append(formattedOutput);
            currentChange.AppendToPatch(formattedOutput);

            return 0;
        }

        #region IEnumerable<PatchEntryChanges> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<PatchEntryChanges> GetEnumerator()
        {
            return changes.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Gets the <see cref="ContentChanges"/> corresponding to the specified <paramref name="path"/>.
        /// </summary>
        public virtual PatchEntryChanges this[string path]
        {
            get { return this[(FilePath)path]; }
        }

        private PatchEntryChanges this[FilePath path]
        {
            get
            {
                PatchEntryChanges entryChanges;
                if (changes.TryGetValue(path, out entryChanges))
                {
                    return entryChanges;
                }

                return null;
            }
        }

        /// <summary>
        /// The total number of lines added in this diff.
        /// </summary>
        public virtual int LinesAdded
        {
            get { return linesAdded; }
        }

        /// <summary>
        /// The total number of lines deleted in this diff.
        /// </summary>
        public virtual int LinesDeleted
        {
            get { return linesDeleted; }
        }

        /// <summary>
        /// The full patch file of this diff.
        /// </summary>
        public virtual string Content
        {
            get { return fullPatchBuilder.ToString(); }
        }

        /// <summary>
        /// Implicit operator for string conversion.
        /// </summary>
        /// <param name="patch"><see cref="Patch"/>.</param>
        /// <returns>The patch content as string.</returns>
        public static implicit operator string(Patch patch)
        {
            return patch.fullPatchBuilder.ToString();
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "+{0} -{1}",
                                     linesAdded,
                                     linesDeleted);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // This doesn't do anything yet because it loads everything
            // eagerly and disposes of the diff handle in the constructor.
        }
    }
}
