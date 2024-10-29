using NUnit.Framework;
using System.IO;
using System.Threading;

namespace Frends.HIT.Email.Tests
{
    /// <summary>
    /// NOTE: To run these unit tests, you need an SMTP test server.
    /// 
    /// docker run -p 3000:80 -p 2525:25 -d rnwood/smtp4dev:v3
    /// 
    /// Management console can be found from http://localhost:3000/
    /// 
    /// </summary>
    [TestFixture]
    public class SendEmailTests
    {
        private const string USERNAME = "test";
        private const string PASSWORD = "test";
        private const string SMTPADDRESS = "localhost";
        private const string TOEMAILADDRESS = "test@test.com";
        private const string FROMEMAILADDRESS = "user@user.com";
        private const int PORT = 2525;
        private const bool ACCEPTALLCERTS = true;
        private const string TEMP_ATTACHMENT_SOURCE = "emailtestattachments";
        private const string TEST_FILE_NAME = "testattachment.txt";
        private const string TEST_FILE_NOT_EXISTING = "doesntexist.txt";

        private string _localAttachmentFolder;
        private string _filepath;
        private Input _input;
        private Options _options;

        [SetUp]
        public void EmailTestSetup()
        {
            _localAttachmentFolder = Path.Combine(Path.GetTempPath(), TEMP_ATTACHMENT_SOURCE);

            if (!Directory.Exists(_localAttachmentFolder)) Directory.CreateDirectory(_localAttachmentFolder);

            _filepath = Path.Combine(_localAttachmentFolder, TEST_FILE_NAME);

            if (!File.Exists(_filepath)) using (File.Create(_filepath)) { }

            _input = new Input()
            {
                From = FROMEMAILADDRESS,
                To = TOEMAILADDRESS,
                Cc = "",
                Bcc = "",
                Message = "testmsg",
                IsMessageHtml = false,
                SenderName = "EmailTestSender",
                MessageEncoding = "utf-8"
            };

            _options = new Options()
            {
                UserName = USERNAME,
                Password = PASSWORD,
                SMTPServer = SMTPADDRESS,
                Port = PORT,
                SecureSocket = SecureSocketOption.Auto,
                AcceptAllCerts = ACCEPTALLCERTS,
                SkipAuthentication = true
            };

        }
        [TearDown]
        public void EmailTestTearDown()
        {
            if (Directory.Exists(_localAttachmentFolder)) Directory.Delete(_localAttachmentFolder, true);
        }

        [Test]
        public void SendEmailWithPlainText()
        {
            var input = _input;
            input.Subject = "Email test - PlainText";
            
            var result = EmailTask.SendEmail(input, null, _options, new CancellationToken());
            Assert.IsTrue(result.EmailSent);
        }

        [Test]
        public void SendEmailWithFileAttachment()
        {
            var input = _input;
            input.Subject = "Email test - FileAttachment";
            
            var attachment = new Attachment
            {
                FilePath = _filepath,
                SendIfNoAttachmentsFound = false,
                ThrowExceptionIfAttachmentNotFound = true
            };


            var Attachments = new Attachment[] { attachment };

            var result = EmailTask.SendEmail(input, Attachments, _options, new CancellationToken());
            Assert.IsTrue(result.EmailSent);
        }

        [Test]
        public void SendEmailWithStringAttachment()
        {
            var input = _input;
            input.Subject = "Email test - AttachmentFromString";
            var fileAttachment = new AttachmentFromString() { FileContent = "teststring ä ö", FileName = "testfilefromstring.txt" };
            var attachment = new Attachment()
            {
                AttachmentType = AttachmentType.AttachmentFromString,
                StringAttachment = fileAttachment
            };
            var Attachments = new Attachment[] { attachment};

            var result = EmailTask.SendEmail(input, Attachments, _options, new CancellationToken());
            Assert.IsTrue(result.EmailSent);
        }

        [Test]
        public void TrySendingEmailWithNoFileAttachmentFound()
        {
            var input = _input;
            input.Subject = "Email test";
            
            var attachment = new Attachment
            {
                FilePath = Path.Combine(_localAttachmentFolder, TEST_FILE_NOT_EXISTING),
                SendIfNoAttachmentsFound = false,
                ThrowExceptionIfAttachmentNotFound = false
            };


            var Attachments = new Attachment[] { attachment };

            var result = EmailTask.SendEmail(input, Attachments, _options, new CancellationToken());
            Assert.IsFalse(result.EmailSent);
        }

        [Test]
        public void TrySendingEmailWithNoFileAttachmentFoundException()
        {
            var input = _input;
            input.Subject = "Email test";
            
            var attachment = new Attachment
            {
                FilePath = Path.Combine(_localAttachmentFolder, TEST_FILE_NOT_EXISTING),
                SendIfNoAttachmentsFound = false,
                ThrowExceptionIfAttachmentNotFound = true
            };


            var Attachments = new Attachment[] { attachment };

            Assert.Throws<FileNotFoundException>(() => EmailTask.SendEmail(input, Attachments, _options, new CancellationToken()));
            
        }

        [Test]
        public void SendEmailWithAuthentication()
        {
            var input = _input;
            input.Subject = "Email test - Authentication";

            var options = _options;
            options.SkipAuthentication = false;
            options.UserName = "Something";
            options.Password = "Something";

            var result = EmailTask.SendEmail(input, null, _options, new CancellationToken());
            Assert.IsTrue(result.EmailSent);
        }

        [Test]
        public void SendEmailWithCustomHeaders()
        {
            var input = _input;
            input.Subject = "Email test - CustomHeaders";

            var headers = new Header[]
            {
                new Header { Key = "Content-Transfer-Encoding", Value = "7bit" }
            };

            var options = _options;
            options.CustomHeaders = headers;

            var result = EmailTask.SendEmail(input, null, options, new CancellationToken());
            Assert.IsTrue(result.EmailSent);
        }
    }
}
