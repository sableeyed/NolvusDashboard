namespace Nolvus.Core.Interfaces
{
    public interface IBSArchService
    {
        /// <summary>
        /// Unpacks a BSA file into an output directory.
        /// </summary>
        /// <param name="bsaFile">The full path to the BSA file.</param>
        /// <param name="outputDirectory">Directory to extract into.</param>
        /// <returns>true if successful; false otherwise</returns>
        Task<bool> UnpackAsync(string bsaFile, string outputDirectory);

        /// <summary>
        /// Packs a directory into a BSA file.
        /// </summary>
        Task<bool> PackAsync(string sourceDirectory, string outputFile);

        /// <summary>
        /// Raw execution wrapper, used internally by implementations.
        /// </summary>
        Task<int> RunBSArchAsync(string arguments);
    }
}
