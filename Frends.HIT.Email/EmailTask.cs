using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System;
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Graph;
using System.Text;
using File = System.IO.File;
using Directory = System.IO.Directory;
using System.Linq;

#pragma warning disable 1591

namespace Frends.HIT.Email
{
    public class EmailTask
    {
        /// <summary>
        /// Sends email message with optional attachments. See https://github.com/Hoglandets-IT/Frends.HIT.Email
        /// </summary>
        /// <returns>
        /// Object { bool EmailSent, string StatusString }
        /// </returns>
        public static Output SendEmail([PropertyTab]Input message, [PropertyTab]Attachment[] attachments, [PropertyTab]Options SMTPSettings, CancellationToken cancellationToken)
        {
            var output = new Output();
            var mail = CreateMimeMessage(message, SMTPSettings.CustomHeaders);

            if (attachments != null && attachments.Length > 0)
            {
                // Email object is created using BodyBuilder.
                var builder = new BodyBuilder();

                if (message.IsMessageHtml) builder.HtmlBody = message.Message;
                else builder.TextBody = message.Message;

                foreach (var attachment in attachments)
                {
                    if (attachment.AttachmentType == AttachmentType.FileAttachment)
                    {
                        var allAttachmentFilePaths = GetAttachmentFiles(attachment.FilePath);

                        if (attachment.ThrowExceptionIfAttachmentNotFound && allAttachmentFilePaths.Count == 0) throw new FileNotFoundException(string.Format("The given filepath \"{0}\" had no matching files", attachment.FilePath), attachment.FilePath);

                        if (allAttachmentFilePaths.Count == 0 && !attachment.SendIfNoAttachmentsFound)
                        {
                            output.StatusString = $"No attachments found matching path \"{attachment.FilePath}\". No email sent.";
                            output.EmailSent = false;
                            return output;
                        }

                        foreach (var filePath in allAttachmentFilePaths)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            builder.Attachments.Add(filePath);
                        }

                        continue;
                    }

                    byte[] byteContent = new byte[]{};
                    string fileName = "";
                                
                    if (attachment.AttachmentType == AttachmentType.AttachmentFromString) {
                        switch (attachment.StringAttachment.Encoding) {
                            case StringEncodingTypes.UTF8:
                                byteContent = System.Text.Encoding.UTF8.GetBytes(attachment.StringAttachment.FileContent);
                                break;
                            case StringEncodingTypes.ISO88591:
                                byteContent = System.Text.Encoding.GetEncoding("ISO8859-1").GetBytes(attachment.StringAttachment.FileContent);
                                break;
                            case StringEncodingTypes.LATIN1:
                                byteContent = System.Text.Encoding.GetEncoding("Latin1").GetBytes(attachment.StringAttachment.FileContent);
                                break;
                            case StringEncodingTypes.ASCII:
                                byteContent = System.Text.Encoding.ASCII.GetBytes(attachment.StringAttachment.FileContent);
                                break;
                            default:
                                return new Output
                                {
                                    EmailSent = false,
                                    StatusString = $"Invalid encoding set for attachment " + attachment.StringAttachment.FileName
                                };
                        }

                        fileName = attachment.StringAttachment.FileName;
                    }
                    else if (attachment.AttachmentType == AttachmentType.AttachmentFromBytes) {
                        byteContent = attachment.ByteAttachment.FileContent;
                        fileName = attachment.ByteAttachment.FileName;
                    }
                    
                    if (byteContent.Length == 0 && (attachment.ThrowExceptionIfAttachmentNotFound || !attachment.SendIfNoAttachmentsFound)) {
                        return new Output{
                            EmailSent = false,
                            StatusString = "Invalid attachment " + fileName + " - no content found"
                        };
                    }

                    builder.Attachments.Add(fileName, byteContent);
                }

                mail.Body = builder.ToMessageBody();
            }
            else
            {
                mail.Body = (message.IsMessageHtml) 
                    ? new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Message } 
                    : new TextPart(MimeKit.Text.TextFormat.Plain) { Text = message.Message };
            }

            // Initialize new MailKit SmtpClient.
            using (var client = new SmtpClient())
            {
                // Accept all certs?
                if (SMTPSettings.AcceptAllCerts) client.ServerCertificateValidationCallback = (s, x509certificate, x590chain, sslPolicyErrors) => true;
                else client.ServerCertificateValidationCallback = MailService.DefaultServerCertificateValidationCallback;

                var secureSocketOption = new SecureSocketOptions();
                switch (SMTPSettings.SecureSocket)
                {
                    case SecureSocketOption.None:
                        secureSocketOption = SecureSocketOptions.None;
                        break;
                    case SecureSocketOption.SslOnConnect:
                        secureSocketOption = SecureSocketOptions.SslOnConnect;
                        break;
                    case SecureSocketOption.StartTls:
                        secureSocketOption = SecureSocketOptions.StartTls;
                        break;
                    case SecureSocketOption.StartTlsWhenAvailable:
                        secureSocketOption = SecureSocketOptions.StartTlsWhenAvailable;
                        break;
                    default:
                        secureSocketOption = SecureSocketOptions.Auto;
                        break;
                }

                client.Connect(SMTPSettings.SMTPServer, SMTPSettings.Port, secureSocketOption);


                if (!SMTPSettings.SkipAuthentication)
                {
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Authenticate(new NetworkCredential(SMTPSettings.UserName, SMTPSettings.Password));
                }

                client.Send(mail);

                client.Disconnect(true);

                client.Dispose();
            }

            output.EmailSent = true;
            output.StatusString = string.Format("Email sent to: {0}", mail.To.ToString());

            return output;
        }

        /// <summary>
        /// Sends email message to Exchange with optional attachments. See https://github.com/Hoglandets-IT/Frends.HIT.Email
        /// </summary>
        /// <returns>
        /// Object { bool EmailSent, string StatusString }
        /// </returns>
        public static async Task<Output> SendEmailToExchangeServer([PropertyTab] ExchangeInput input, [PropertyTab] Attachment[] attachments, [PropertyTab] ExchangeServer settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(settings.AppId) || string.IsNullOrWhiteSpace(settings.TenantId) || string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
                throw new ArgumentException("Invalid Application ID, Tenant ID, Username or Password. Please check Exchange settings.");

            if (string.IsNullOrWhiteSpace(input.Subject) || string.IsNullOrWhiteSpace(input.Message) || string.IsNullOrWhiteSpace(input.To))
                throw new ArgumentException("Subject, message, and To-recipient cannot be empty.");

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var credentials = new UsernamePasswordCredential(settings.Username, settings.Password, settings.TenantId, settings.AppId, options);
            var graph = new GraphServiceClient(credentials);

            var messageBody = new ItemBody
            {
                ContentType = input.IsMessageHtml ? BodyType.Html : BodyType.Text,
                Content = input.Message
            };

            var recipients = input.To.Split(new char[] { ',', ';'}, StringSplitOptions.RemoveEmptyEntries);

            var to = new List<Recipient>();
            var cc = new List<Recipient>();
            var bcc = new List<Recipient>();

            foreach (var receiver in recipients) to.Add(new Recipient { EmailAddress = new EmailAddress { Address = receiver } });

            if (!string.IsNullOrWhiteSpace(input.Cc))
            {
                recipients = input.Cc.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var receiver in recipients) cc.Add(new Recipient { EmailAddress = new EmailAddress { Address = receiver } });
            }
            if (!string.IsNullOrWhiteSpace(input.Bcc))
            {
                recipients = input.Bcc.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var receiver in recipients) bcc.Add(new Recipient { EmailAddress = new EmailAddress { Address = receiver } });
            }

            if (attachments != null && attachments.Length > 0)
            {
                return await SendExchangeEmailWithAttachments(attachments, graph, input.Subject, messageBody, to, cc, bcc, cancellationToken);
            }
            else
            {
                var message = new Message
                {
                    Subject = input.Subject,
                    Body = messageBody,
                    ToRecipients = to,
                    CcRecipients = cc,
                    BccRecipients = bcc
                };
                if (string.IsNullOrWhiteSpace(input.From))
                    await graph.Me.SendMail(message, true).Request().PostAsync(cancellationToken);
                else
                    await graph.Users[input.From].SendMail(message, true).Request().PostAsync(cancellationToken);
                return new Output
                {
                    EmailSent = true,
                    StatusString = $"Email sent to: {input.To}"
                };
            }
        }

        #region HelperMethods

        private static async Task<Output> SendExchangeEmailWithAttachments(Attachment[] attachments, GraphServiceClient graphClient, string subject, ItemBody messageBody, List<Recipient> to, List<Recipient> cc, List<Recipient> bcc, CancellationToken cancellationToken)
        {
            var message = new Message
            {
                Subject = subject,
                Body = messageBody,
                ToRecipients = to,
                CcRecipients = cc,
                BccRecipients = bcc
            };

            var msgResult = await graphClient.Me.Messages.Request().AddAsync(message, cancellationToken);

            foreach (var attachment in attachments)
            {
                

                if (attachment.AttachmentType == AttachmentType.FileAttachment) {
                    var attachmentList = new MessageAttachmentsCollectionPage();
                    var allAttachmentFilePaths = new List<string>();

                    allAttachmentFilePaths = GetAttachmentFiles(attachment.FilePath);
                    if (allAttachmentFilePaths.Count == 0 && !attachment.SendIfNoAttachmentsFound)
                    {
                        return new Output
                        {
                            EmailSent = false,
                            StatusString = $"No attachments found matching path \"{attachmentList[0].Name}\". No email sent."
                        };
                    }

                    if (attachment.ThrowExceptionIfAttachmentNotFound && allAttachmentFilePaths.Count == 0) throw new FileNotFoundException($"The given filepath \"{attachment.FilePath}\" had no matching files");
                    
                    foreach (var filePath in allAttachmentFilePaths)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var info = new FileInfo(filePath);
                        var attachmentSubItem = new AttachmentItem
                        {
                            AttachmentType = Microsoft.Graph.AttachmentType.File,
                            Name = Path.GetFileName(filePath),
                            Size = info.Length
                        };

                        var uploadSubSession = await graphClient.Me.Messages[msgResult.Id].Attachments.CreateUploadSession(attachmentSubItem).Request().PostAsync(cancellationToken);
                        var allBytes = File.ReadAllBytes(filePath);

                        using (var stream = new MemoryStream(allBytes))
                        {
                            stream.Position = 0;
                            var largeFileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSubSession, stream);
                            await largeFileUploadTask.UploadAsync();
                        }
                    }

                    continue;
                }

                byte[] byteContent = new byte[]{};
                string fileName = "";
                               
                if (attachment.AttachmentType == AttachmentType.AttachmentFromString) {
                    switch (attachment.StringAttachment.Encoding) {
                        case StringEncodingTypes.UTF8:
                            byteContent = System.Text.Encoding.UTF8.GetBytes(attachment.StringAttachment.FileContent);
                            break;
                        case StringEncodingTypes.ISO88591:
                            byteContent = System.Text.Encoding.GetEncoding("ISO8859-1").GetBytes(attachment.StringAttachment.FileContent);
                            break;
                        case StringEncodingTypes.LATIN1:
                            byteContent = System.Text.Encoding.GetEncoding("Latin1").GetBytes(attachment.StringAttachment.FileContent);
                            break;
                        case StringEncodingTypes.ASCII:
                            byteContent = System.Text.Encoding.ASCII.GetBytes(attachment.StringAttachment.FileContent);
                            break;
                        default:
                            return new Output
                            {
                                EmailSent = false,
                                StatusString = $"Invalid encoding set for attachment " + attachment.StringAttachment.FileName
                            };
                    }

                    fileName = attachment.StringAttachment.FileName;
                }
                else if (attachment.AttachmentType == AttachmentType.AttachmentFromBytes) {
                    byteContent = attachment.ByteAttachment.FileContent;
                    fileName = attachment.ByteAttachment.FileName;
                }
                
                if (byteContent.Length == 0 && (attachment.ThrowExceptionIfAttachmentNotFound || !attachment.SendIfNoAttachmentsFound)) {
                    return new Output{
                        EmailSent = false,
                        StatusString = "Invalid attachment " + fileName + " - no content found"
                    };
                }

                cancellationToken.ThrowIfCancellationRequested();
                
                var attachmentItem = new AttachmentItem
                {
                    AttachmentType = Microsoft.Graph.AttachmentType.File,
                    Name = attachment.ByteAttachment.FileName,
                    Size = attachment.ByteAttachment.FileContent.Length
                };

                var uploadSession = await graphClient.Me.Messages[msgResult.Id].Attachments.CreateUploadSession(attachmentItem).Request().PostAsync(cancellationToken);
                using (var fileStream = new MemoryStream(attachment.ByteAttachment.FileContent)) {
                    fileStream.Position = 0;
                    var largeFileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, fileStream);
                    await largeFileUploadTask.UploadAsync();
                }
                
            }

            await graphClient.Me.Messages[msgResult.Id].Send().Request().PostAsync(cancellationToken);
            return new Output
            {
                EmailSent = true,
                StatusString = $"Email sent to: {to}"
            };
        }

        /// <summary>
        /// Create MimeMessage.
        /// </summary>
        private static MimeMessage CreateMimeMessage(Input message, Header[] headers)
        {
            // Split recipients, either by comma or semicolon.
            var separators = new[] { ',', ';' };

            var recipients = message.To.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var ccRecipients = message.Cc.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var bccRecipients = message.Bcc.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // Create mail object.
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress(message.SenderName, message.From));
            mail.Subject = message.Subject;

            // Add recipients.
            foreach (var recipientAddress in recipients)
                mail.To.Add(MailboxAddress.Parse(recipientAddress));

            // Add CC recipients.
            foreach (var ccRecipient in ccRecipients)
                mail.Cc.Add(MailboxAddress.Parse(ccRecipient));

            // Add BCC recipients.
            foreach (var bccRecipient in bccRecipients)
                mail.Bcc.Add(MailboxAddress.Parse(bccRecipient));

            if (headers != null && headers.Length > 0)
            {
                foreach (var header in headers)
                    mail.Headers.Add(header.Key, header.Value);
            }

            return mail;
        }

        /// <summary>
        /// Gets all actual file names of attachments matching given file path.
        /// </summary>
        /// <param name="filePath"></param>
        private static List<string> GetAttachmentFiles(string filePath)
        {
            var folder = Path.GetDirectoryName(filePath);
            var fileMask = Path.GetFileName(filePath) != "" ? Path.GetFileName(filePath) : "*";
            var filePaths = Directory.GetFiles(folder, fileMask);
            return filePaths.ToList();
        }

        #endregion
    }
}
