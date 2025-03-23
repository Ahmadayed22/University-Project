public class AcceptanceRecord
{
    public int? RecordID { get; set; }  // New Primary Key
    private bool _isAccepted;
    private DateOnly _date;
    private List<string> _reason;
    private bool _finalized;

    public AcceptanceRecord(bool isAccepted, DateOnly date, List<string> reason, int? recordID = null, bool finalized = false)
    {
        _isAccepted = isAccepted;
        _date = date;
        _reason = reason;
        RecordID = recordID; // Assign the new primary key if available
        _finalized = finalized;
    }

    public bool IsAccepted
    {
        get => _isAccepted;
        set => _isAccepted = value;
    }

    public bool Finalized
    {
        get => _finalized;
        set => _finalized = value;
    }

    public DateOnly Date
    {
        get => _date;
        set => _date = value;
    }

    public List<string> Reason
    {
        get => _reason;
        set => _reason = value;
    }

 
    public override string ToString()
    {
        return $"{(RecordID.HasValue ? $"RecordID: {RecordID}, " : "")}{_isAccepted} on {_date} because {string.Join(", ", _reason)}";
    }
}
