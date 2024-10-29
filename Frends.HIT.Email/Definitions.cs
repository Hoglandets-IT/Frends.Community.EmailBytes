using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace Frends.HIT.Email
{
    public class Input
    {
        /// <summary>
        /// Recipient addresses separated by ',' or ';'.
        /// </summary>
        [DefaultValue("\"jane.doe@somedomain.com\"")]
        public string To { get; set; }

        /// <summary>
        /// Cc recipient addresses separated by ',' or ';'.
        /// </summary>
        [DefaultValue("")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Cc { get; set; }

        /// <summary>
        /// Bcc recipient addresses separated by ',' or ';'.
        /// </summary>
        [DefaultValue("")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Bcc { get; set; }

        /// <summary>
        /// Sender address.
        /// </summary>
        [DefaultValue("\"john.doe@somedomain.com\"")]
        public string From { get; set; }

        /// <summary>
        /// Name of the sender.
        /// </summary>
        [DefaultValue("\"\"")]
        public string SenderName { get; set; }

        /// <summary>
        /// Email message's subject.
        /// </summary>
        [DefaultValue("\"Hello Jane\"")]
        public string Subject { get; set; }

        /// <summary>
        /// Body of the message.
        /// </summary>
        [DefaultValue("\"You've got mail!\"")]
        public string Message { get; set; }

        /// <summary>
        /// Set this true if the message is HTML.
        /// </summary>
        [DefaultValue("false")]
        public bool IsMessageHtml { get; set; }

        /// <summary>
        /// Encoding of message body and subject.
        /// Use following table's name column for other options: https://msdn.microsoft.com/en-us/library/system.text.encoding(v=vs.110).aspx#Anchor_5 
        /// </summary>
        [DefaultValue("\"utf-8\"")]
        public string MessageEncoding { get; set; }

    }

    public class ExchangeInput
    {
        /// <summary>
        /// Recipient addresses separated by ',' or ';'.
        /// </summary>
        [DefaultValue("\"jane.doe@somedomain.com\"")]
        public string To { get; set; }

        /// <summary>
        /// Cc recipient addresses separated by ',' or ';'.
        /// </summary>
        [DefaultValue("")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Cc { get; set; }

        /// <summary>
        /// Bcc recipient addresses separated by ',' or ';'.
        /// </summary>
        [DefaultValue("")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Bcc { get; set; }

        /// <summary>
        /// Optional.
        /// Sender email address.
        /// If left empty, logged in user email address will be used.
        /// </summary>
        [DefaultValue("john.doe@somedomain.com")]
        [DisplayFormat(DataFormatString = "Text")]
        public string From { get; set; }

        /// <summary>
        /// Email message's subject.
        /// </summary>
        [DefaultValue("\"Hello World\"")]
        public string Subject { get; set; }

        /// <summary>
        /// Body of the message.
        /// </summary>
        [DefaultValue("\"You've got mail!\"")]
        public string Message { get; set; }

        /// <summary>
        /// Set this true if the message is HTML.
        /// </summary>
        [DefaultValue("false")]
        public bool IsMessageHtml { get; set; }
    }

    public class Options
    {
        /// <summary>
        /// SMTP server address.
        /// </summary>
        [DefaultValue("\"smtp.somedomain.com\"")]
        public string SMTPServer { get; set; }

        /// <summary>
        /// SMTP server port.
        /// </summary>
        [DefaultValue("25")]
        public int Port { get; set; }

        /// <summary>
        /// Choose the SecureSocketOption to use, default is Auto
        /// </summary>
        [DefaultValue(SecureSocketOption.Auto)]
        public SecureSocketOption SecureSocket { get; set; }

        /// <summary>
        /// Should the task accept all certificates from IMAP server, including invalid ones?
        /// </summary>
        [DefaultValue(false)]
        public bool AcceptAllCerts { get; set; }

        /// <summary>
        /// Do you want to skip authentication to SMTP server?
        /// </summary>
        [DefaultValue(false)]
        public bool SkipAuthentication { get; set; }

        /// <summary>
        /// Use this username to log in to the SMTP server.
        /// </summary>
        [DefaultValue("\"\"")]
        [UIHint(nameof(SkipAuthentication), "", false)]
        public string UserName { get; set; }

        /// <summary>
        /// Use this password to log in to the SMTP server.
        /// </summary>
        [PasswordPropertyText]
        [DefaultValue("\"\"")]
        [UIHint(nameof(SkipAuthentication), "", false)]
        public string Password { get; set; }

        /// <summary>
        /// Custom headers for email sending.
        /// </summary>
        public Header[] CustomHeaders { get; set; }
    }

    /// <summary>
    /// Header class for SendEmail-Task's options.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Header key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Header value.
        /// </summary>
        public string Value { get; set; }
    }

    public class ExchangeServer
    {
        /// <summary>
        /// Username of the user.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password for the user.
        /// </summary>
        [PasswordPropertyText]
        public string Password { get; set; }

        /// <summary>
        /// App ID for fetching access token.
        /// </summary>
        [DefaultValue("")]
        public string AppId { get; set; }

        /// <summary>
        /// Tenant ID for fetching access token.
        /// </summary>
        [DefaultValue("")]
        public string TenantId { get; set; }
    }

    public class Output
    {
        /// <summary>
        /// Value is true if email was sent.
        /// </summary>
        public bool EmailSent { get; set; }
        /// <summary>
        /// Contains information about the task's result.
        /// </summary>
        public string StatusString { get; set; }
    }

    public class Attachment
    {
        /// <summary>
        /// Chooses if the attachment file is created from a string or copied from disk.
        /// </summary>
        public AttachmentType AttachmentType { get; set; }

        [UIHint(nameof(AttachmentType), "", AttachmentType.AttachmentFromString)]
        public AttachmentFromString StringAttachment { get; set; }

        [UIHint(nameof(AttachmentType), "", AttachmentType.AttachmentFromBytes)]
        public AttachmentFromBytes ByteAttachment { get; set; }

        /// <summary>
        /// Attachment file's path
        /// Uses Directory.GetFiles(string, string) as a pattern matching technique.
        /// See https://msdn.microsoft.com/en-us/library/wz42302f(v=vs.110).aspx.
        /// Exception: If the path ends in a directory, all files in that folder are added as attachments.
        /// </summary>
        [DefaultValue("\"\"")]
        [UIHint(nameof(AttachmentType), "", AttachmentType.FileAttachment)]
        public string FilePath { get; set; }

        /// <summary>
        /// If set true and no files match the given path, an exception is thrown.
        /// </summary>
        [UIHint(nameof(AttachmentType), "", AttachmentType.FileAttachment)]
        public bool ThrowExceptionIfAttachmentNotFound { get; set; }

        /// <summary>
        /// If set true and no files match the given path, email will be sent nevertheless.
        /// </summary>
        [UIHint(nameof(AttachmentType), "", AttachmentType.FileAttachment)]
        public bool SendIfNoAttachmentsFound { get; set; }
    }
    public enum AttachmentType
    {
        /// <summary>
        /// Select this if the attachment is a file.
        /// </summary>
        FileAttachment,

        /// <summary>
        /// Select this if the attachment file should be created from a string.
        /// </summary>
        AttachmentFromString,

        /// <summary>
        /// Include a raw byte stream as an attachment
        /// </summary>
        AttachmentFromBytes
    }

    public enum SecureSocketOption
    {
        /// <summary>
        /// No SSL or TLS encryption should be used. 
        /// </summary>
        None,
        /// <summary>
        /// Allow the IMailService to decide which SSL or TLS options to use (default). 
        /// If the server does not support SSL or TLS, then the connection will continue without any encryption.
        /// </summary>
        Auto,
        /// <summary>
        /// The connection should use SSL or TLS encryption immediately.
        /// </summary>
        SslOnConnect,
        /// <summary>
        /// Elevates the connection to use TLS encryption immediately after reading the greeting and capabilities of 
        /// the server. If the server does not support the STARTTLS extension, then the connection will fail and a 
        /// NotSupportedException will be thrown.
        /// </summary>
        StartTls,
        /// <summary>
        /// Elevates the connection to use TLS encryption immediately after reading the greeting and capabilities of 
        /// the server, but only if the server supports the STARTTLS extension.
        /// </summary>
        StartTlsWhenAvailable
    }

    public enum StringEncodingTypes {
        UTF8,

        ISO88591,

        LATIN1,

        ASCII
    }

    public class AttachmentFromString
    {
        /// <summary>
        /// Content of the attachment file.
        /// </summary>
        [DefaultValue("\"\"")]
        public string FileContent { get; set; }

        /// <summary>
        /// Name of the attachment file.
        /// </summary>
        [DefaultValue("\"\"")]
        [DisplayFormat(DataFormatString = "Text")]
        public string FileName { get; set; }

        /// <summary>
        /// Text Encoding
        /// </summary>
        public StringEncodingTypes Encoding { get; set; }
    }

    public class AttachmentFromBytes
    {
        /// <summary>
        /// Content of the attachment
        /// </summary>
        [DefaultValue(new byte[]{})]
        [DisplayFormat(DataFormatString = "Expression")]
        public byte[] FileContent { get; set; }

        /// <summary>
        /// The filename of the file
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("filename.txt")]
        public string FileName { get; set; }
    }
}
