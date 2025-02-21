namespace SQLScriptRunner.Models;

public class SmtpConfiguration
{
    public string ServerName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsSmtpServer { get; set; } // true for smtp-server and false for smtp-relay
    public string ProtocolName { get; set; } = string.Empty; //eg: "SSL", "TLS"
    public bool IsAuthenticatedRelay { get; set; }
    public string RecipientEmails { get; set; } = string.Empty;
}
