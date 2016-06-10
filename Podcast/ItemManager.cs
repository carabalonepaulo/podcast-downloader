using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Podcast {
    public struct Item {
        public string Title;
        public string Link;
        public string Date;
        public string AudioUrl;
    }

    public struct Source {
        public string Title;
        public string Url;
        public Item [] Items;
    }

    public class ItemManager {
        public delegate void StateHandler (bool e);
        public event StateHandler StateChanged;

        public delegate void DownloadProgressHandler (int pc);
        public event DownloadProgressHandler DownloadProgressChanged;

        public delegate void DownloadAudioHandler ();
        public event DownloadAudioHandler DownloadAudioCompleted;

        public Source [] Sources { get; private set; }

        /// <summary>
        /// Inicia o processo de carregamento.
        /// </summary>
        public async void StartProcess () {
            StateChanged?.Invoke (true);

            Sources = LoadSources ();
            if (!Directory.Exists ("./Cache/"))
                Directory.CreateDirectory ("./Cache/");
            for (int i = 0; i < Sources.Length; i++) {
                Console.WriteLine ($"Carregando {Sources [i].Title}...");

                if (File.Exists ($"./Cache/{Sources [i].Title}.xml")) {
                    Sources [i].Items = LoadItems (File.ReadAllText ($"./Cache/{Sources [i].Title}.xml"));
                } else {
                    await Task.Run (() => {
                        WebClient client = new WebClient ();
                        byte [] buff = client.DownloadData (Sources [i].Url);
                        Sources [i].Items = LoadItems (Encoding.UTF8.GetString (buff));
                    });
                }

                Console.WriteLine ($"{Sources [i].Items.Length} items carregados!");
            }

            StateChanged?.Invoke (false);
        }

        /// <summary>
        /// Adiciona uma nova fonte.
        /// </summary>
        /// <param name="url"></param>
        public void AddSource (Source xsrc) {
            Source [] nsrc = (Source [])this.Sources.Clone ();
            Array.Resize (ref nsrc, nsrc.Length + 1);
            nsrc [nsrc.Length - 1] = xsrc;

            XmlDocument document = new XmlDocument ();
            XmlElement Sources = (XmlElement)document.AppendChild (document.CreateElement ("sources"));
            foreach (Source src in nsrc) {
                XmlElement source = (XmlElement)Sources.AppendChild (document.CreateElement ("source"));
                source.SetAttribute ("title", src.Title);
                source.SetAttribute ("url", src.Url);
                Console.WriteLine ("Adicionando: {0}\n{1}", src.Title, src.Url);
            }

            document.Save ("./sources.xml");
            StartProcess ();
        }

        /// <summary>
        /// Faz download de um arquivo de áudio.
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="itemIndex"></param>
        public WebClient Download (int sourceIndex, int itemIndex) {
            return Download (Sources [sourceIndex].Items [itemIndex]);
        }

        /// <summary>
        /// Faz download de um arquivo de áudio.
        /// </summary>
        /// <param name="item"></param>
        public WebClient Download (Item item) {
            WebClient client = new WebClient ();
            client.DownloadFileAsync (new Uri (item.AudioUrl), $"./Podcasts/{SanitizeFileName (item.Title)}.mp3");
            client.DownloadProgressChanged += (sender, e) => {
                DownloadProgressChanged?.Invoke (e.ProgressPercentage);
                Console.WriteLine ($"Download '{item.Title}': {e.ProgressPercentage}%");
            };
            client.DownloadFileCompleted += (sender, e) => {
                DownloadAudioCompleted?.Invoke ();
                Console.WriteLine ($"Download '{item.Title}' concluído!");
            };
            Console.WriteLine ($"Download '{item.Title}' iniciado!");
            return client;
        }

        /// <summary>
        /// Carrega as fontes dos podcasts a partir de um arquivo XML.
        /// </summary>
        /// <returns></returns>
        Source [] LoadSources () {
            var document = new XmlDocument ();
            document.Load ("./Sources.xml");

            XmlNodeList list = document.SelectNodes ("sources/source");
            Source [] items = new Source [list.Count];

            for (int i = 0; i < items.Length; i++)
                items [i] = new Source () {
                    Title = list [i].Attributes ["title"].InnerText,
                    Url = list[i].Attributes["url"].InnerText
                };

            return items;
        }

        /// <summary>
        /// Carrega os itens de cada podcast a partir de um XML.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        Item [] LoadItems (string xml) {
            XmlDocument document = new XmlDocument ();
            document.LoadXml (xml);

            XmlNodeList list = document.SelectNodes ("rss/channel/item");
            Item [] items = new Item [list.Count];

            for (int i = 0; i < items.Length; i++) {
                if (list [i].SelectSingleNode ("enclosure") == null)
                    continue;

                items [i] = new Item () {
                    Title = list [i].SelectSingleNode ("title").InnerText,
                    Link = list [i].SelectSingleNode ("link").InnerText,
                    Date = list [i].SelectSingleNode ("pubDate").InnerText,
                    AudioUrl = list [i].SelectSingleNode ("enclosure").Attributes ["url"].InnerText
                };
            }

            // Remove itens nulos
            items = items.Where (i => i.Title != null).ToArray ();

            string title = document.SelectSingleNode ("rss/channel/title").InnerText;
            if (!File.Exists ($"./Cache/{title}.xml"))
                File.WriteAllText ($"./Cache/{title}.xml", xml);

            return items;
        }

        /// <summary>
        /// Remove caracteres ilegais do nome do arquivo.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        string SanitizeFileName (string input) {
            char [] invalidChars = Path.GetInvalidFileNameChars ();
            return new string (input.Where (x => !invalidChars.Contains (x)).ToArray ());
        }
    }
}
