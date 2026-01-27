namespace TightWiki.Contracts.DataModels
{
    public class ProcessingInstruction
    {
        public int PageId { get; set; }
        /// <summary>
        /// TightWiki.Contracts.Constants.WikiInstruction
        /// </summary>
        public string Instruction { get; set; } = string.Empty;
    }
}
