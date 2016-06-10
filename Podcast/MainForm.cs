using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Podcast {
    public partial class MainForm : Form {
        ItemManager Manager;

        public MainForm () {
            InitializeComponent ();
            ManageControls (true);

            Manager = new ItemManager ();
            Manager.StateChanged += ManageControls;
            Manager.DownloadProgressChanged += (e) => pgbDownload.Value = e;
            Manager.DownloadAudioCompleted += () => {
                pgbDownload.Value = 0;
                btnDownload.Enabled = true;
                MessageBox.Show ("Download concluído!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Manager.StartProcess ();

            cmbList.SelectedIndexChanged += (sender, e) => ReloadList ();
            btnRefresh.Click += (sender, e) => ReloadSources ();
        }

        /// <summary>
        /// Evento de atualização do ItemManager.
        /// </summary>
        /// <param name="e"></param>
        void ManageControls (bool e) {
            foreach (Control c in Controls)
                c.Enabled = !e;

            if (!e) {
                cmbList.Items.Clear ();
                cmbList.Items.AddRange (Manager.Sources.Select (x => x.Title).ToArray ());
                cmbList.SelectedIndex = 0;
                ReloadList ();
                UseWaitCursor = false;
            } else UseWaitCursor = true;
        }

        /// <summary>
        /// Recarrega a lista de podcasts a partir do item selecionado.
        /// </summary>
        void ReloadList () {
            listItems.Items.Clear ();
            listItems.Items.AddRange (Manager.Sources [cmbList.SelectedIndex].Items
                .Select (x => x.Title)
                .Where (x => x != null)
                .ToArray ());
        }

        /// <summary>
        /// Recarrega a lista de fontes.
        /// </summary>
        void ReloadSources () {
            if (System.IO.Directory.Exists("./Cache")) 
                System.IO.Directory.Delete ("./Cache/", true);
            Manager.StartProcess ();
        }

        /// <summary>
        /// Evento de mudança no termo de pesquisa.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSearch_TextChanged (object sender, System.EventArgs e) {
            listItems.Items.Clear ();
            listItems.Items.AddRange (Manager.Sources [cmbList.SelectedIndex].Items
                .Where (i => i.Title.ToLower ().Contains (txtSearch.Text.ToLower ()))
                .Select (i => i.Title)
                .ToArray ());
        }

        /// <summary>
        /// Evento de click no botão de download.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDownload_Click (object sender, System.EventArgs e) {
            if (!string.IsNullOrWhiteSpace (txtSearch.Text)) {
                Manager.Download (Manager.Sources [cmbList.SelectedIndex].Items
                    .Where (i => i.Title == (string)listItems.SelectedItem)
                    .First ());
            } else { Manager.Download (cmbList.SelectedIndex, listItems.SelectedIndex); }
            btnDownload.Enabled = false;
        }

        /// <summary>
        /// Evento de click do botão de adicionar novas fontes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAdd_Click (object sender, System.EventArgs e) {
            AddForm form = new AddForm (Manager);
            form.ShowDialog ();
        }
    }
}
