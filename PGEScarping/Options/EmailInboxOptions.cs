namespace PGEScarping.Options;

public sealed class EmailInboxOptions
{
    public const string SectionName = "EmailInbox";

    public string ImapHost { get; set; } = "imap.gmail.com";
    public int ImapPort { get; set; } = 993;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
