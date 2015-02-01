using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;

namespace Luma.SimpleEntity
{
    /// <summary>
    /// Custom MSBuild task to delete all generated files created by <see cref="CreateClientFilesTask"/>
    /// </summary>
    public class CleanClientFilesTask : ClientFilesTask
    {
        /// <summary>
        /// Internal implementation of <see cref="ITask.Execute"/> called from base class.
        /// </summary>
        /// <returns><c>true</c> if task succeeds</returns>
        protected override bool ExecuteInternal()
        {
            // Delete the files we created previously and the lists we created to track them
            this.DeletePreviouslyWrittenFiles();

            // A clean wipes out any history files
            this.DeleteCodeGenMetafileLists();

            // Finally, delete the entire Generated_Code folder if it is now empty
            this.DeleteFolderIfEmpty(this.GeneratedCodePath);

            return true;
        }

        /// <summary>
        /// Deletes all the generated files from a prior pass
        /// </summary>
        private void DeletePreviouslyWrittenFiles()
        {
            // Get the list of files we wrote in a prior build.
            IEnumerable<string> files = FilesPreviouslyWritten();

            // Now, scan the list and determine which ones went away
            foreach (var fileName in files)
            {
                if (File.Exists(fileName))
                {
                    LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Deleting_Orphan, fileName));
                    DeleteFileFromVS(fileName);
                    DeleteFolderIfEmpty(Path.GetDirectoryName(fileName));
                }
            }
        }
    }
}
