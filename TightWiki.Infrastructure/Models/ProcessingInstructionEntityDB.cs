namespace DAL.Models
{
    /// <summary>
    /// Entity model for the ProcessingInstruction table in the database.
    /// Stores wiki processing instructions for pages.
    /// </summary>
    public class ProcessingInstructionEntityDB
    {
        public int PageId { get; set; }
        public string Instruction { get; set; } = string.Empty;
    }
}
