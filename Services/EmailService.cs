using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SQLScriptRunner.Common.Enums;
using SQLScriptRunner.Models;

namespace SQLScriptRunner.Services;

internal sealed class EmailService
{
    private readonly AppSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<AppSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public void ComposeEmail(QueryExecutionResponse response, ScriptExecutionLog scriptExecutionLog)
    {
        string subject = response.IsSuccess ? "Script Executed Successfully" : "Script Execution Failed";

        string message = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; color: #333; line-height: 1.6; }}
                .container {{ padding: 20px; border: 1px solid #ddd; border-radius: 8px; background-color: #f9f9f9; }}
                h2 {{ color: #2c3e50; }}
                .details {{ background-color: #fff; padding: 15px; border-radius: 5px; box-shadow: 0px 0px 5px #ccc; }}
                .footer {{ font-size: 12px; color: #777; margin-top: 20px; }}
                .disclaimer {{ font-size: 11px; color: #888; margin-top: 10px; border-top: 1px solid #ddd; padding-top: 10px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Dear Team,</h2>
                <p>The script has been {(response.IsSuccess ? "executed successfully" : "failed")}. Below are the execution details:</p>

                <div class='details'>
                    <h3>Script Execution Details:</h3>
                    <ul>
                        <li><strong>Execution Date:</strong> {scriptExecutionLog.ExecutionDate}</li>
                        <li><strong>Script Version:</strong> {scriptExecutionLog.ScriptVersion}</li>
                        <li><strong>Execution Status:</strong> {scriptExecutionLog.Status}</li>
                        <li><strong>Executed Till:</strong> {scriptExecutionLog.ExecutedTill}</li>
                    </ul>
                    {(response.IsSuccess ? "<p>No errors were encountered during execution.</p>" : $"<p><strong>Error:</strong> {response.ErrorMessage}</p>")}
                </div>

                <p class='footer'><strong>This is an auto-generated email. Please do not reply to this email.</strong></p>
                <p>If you have any concerns, contact the support team.</p>

                <p class='disclaimer'><strong>Disclaimer:</strong> This email and any files transmitted with it are confidential and intended solely for the use of the individual or entity to whom they are addressed. If you have received this email in error, please notify the sender immediately and delete this email from your system.</p>
            </div>
        </body>
        </html>";

        SendEmail(subject, message);
    }

    private void SendEmail(string subject, string message)
    {
        // Create the email message
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SmtpConfig.SenderName, _settings.SmtpConfig.SenderEmail));

        // Handle multiple recipients
        var recipients = _settings.SmtpConfig.RecipientEmails.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var recipient in recipients)
        {
            email.To.Add(new MailboxAddress(recipient.Trim(), recipient.Trim()));
        }

        email.Subject = subject;

        // Set the email body
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message
        };

        email.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();
        try
        {
            var sslOrTls = (String.Equals(_settings.SmtpConfig.ProtocolName, "ssl", StringComparison.OrdinalIgnoreCase)) ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            // Connect to the SMTP server
           smtpClient.Connect(_settings.SmtpConfig.ServerName, _settings.SmtpConfig.Port,  sslOrTls);

            // Handle SMTP relay configurations
            if (!_settings.SmtpConfig.IsSmtpServer)
            {
                smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");
                if (_settings.SmtpConfig.IsAuthenticatedRelay)
                {
                    smtpClient.Authenticate(_settings.SmtpConfig.UserName, _settings.SmtpConfig.Password);
                }
            }
            else
            {
                smtpClient.Authenticate(_settings.SmtpConfig.UserName, _settings.SmtpConfig.Password);
            }
            // Send the email
            smtpClient.Send(email);

            Console.WriteLine("Email Sent Successfully!");
            _logger.LogInformation((int)EventIds.EmailSentSuccessfully, "Email Sent Successfully!");

        }
        catch (SmtpCommandException ex)
        {
            Console.WriteLine($"SMTP Command Error: {ex.Message}");
            Console.WriteLine($"Status Code: {ex.StatusCode}");
        }
        catch (SmtpProtocolException ex)
        {
            Console.WriteLine($"SMTP Protocol Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
        finally
        {
            // Disconnect from the SMTP server
            smtpClient.Disconnect(true);
        }
    }
}
