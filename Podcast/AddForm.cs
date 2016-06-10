using System;
using System.Windows.Forms;
using System.Xml;
using System.Linq;

namespace Podcast {
    public partial class AddForm : Form {
        ItemManager Manager;

        public AddForm (ItemManager manager) {
            InitializeComponent ();
            Manager = manager;
        }

        private void btnAdd_Click (object sender, EventArgs e) {
            progressBar1.MarqueeAnimationSpeed = 30;
            progressBar1.Style = ProgressBarStyle.Marquee;

            System.Net.WebClient client = new System.Net.WebClient ();
            client.DownloadDataAsync (new Uri (txtAddress.Text));
            client.DownloadDataCompleted += (s, d) => {
                if (d.Error != null) {
                    MessageBox.Show (d.Error.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close ();
                    return;
                }

                if (Manager.Sources.Where(i => i.Url == txtAddress.Text).ToArray().Length != 0) {
                    MessageBox.Show ("Podcast já registrado!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close ();
                    return;
                }

                XmlDocument document = new XmlDocument ();
                document.LoadXml (System.Text.Encoding.UTF8.GetString (d.Result));

                string title = document.SelectSingleNode ("rss/channel/title").InnerText;
                document.Save ($"./Cache/{title}.xml");

                Manager.AddSource (new Source () {
                    Title = title,
                    Url = txtAddress.Text
                });

                Close ();
            };
        }
    }
}
