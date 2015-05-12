using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace TestRig
{
    class Notifier
    {
        private string host;
        private int port = 465;
        private string user;
        private string password;
        private string from;
        private string receipts;

        public Notifier(string host, string user, string password, string from, string receipts)
        {
            this.host = host;
            this.user = user;
            this.password = password;
            this.from = from;
            this.receipts = receipts;
        }

        public void SendMail(string subject, string body)
        {
            SmtpClient client = new SmtpClient();
            //client.Port = this.port;
            client.Host = this.host;
            //client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential(this.user, this.password);

            MailMessage mm = new MailMessage(from, receipts, subject, body);
            mm.BodyEncoding = UTF8Encoding.UTF8;
            mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            client.Send(mm);
        }

        public void SendReceipt(TestReceipt r)
        {
            String subject = "[TestRig] " + r.testDescription.testDescription;

            if (r.testPass)
            {
                subject += " passed";
            }
            else
            {
                subject += " failed";
            }

            SendMail(subject, r.ToString());
        }
    }
}
