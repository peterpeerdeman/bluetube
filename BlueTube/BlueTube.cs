using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.YouTube;
using Google.GData.Photos;
using Google.GData.Extensions.MediaRss;

namespace BlueTube
{

    public partial class BlueTube : Form
    {
        ObexListener ol;
        BluetoothClient bc;

        //Picasa vars
        public PicasaService picasaService;
        public const string albumid = "default";

        //Youtube vars
        private YouTubeRequestSettings settings;
        public YouTubeRequest request;
        public AuthSubUtil auth;
        private const string clientID = "ytapi-GogbotBluetube-BlueTube-jng432tu-0";
        private const string developerKey = "xxxx";
        private const string username = "gogbotbluetube@gmail.com";
        private const string password = "xxxx";

        public YouTubeRequest initializeYoutube()
        {
            settings = new YouTubeRequestSettings("BlueTube", clientID, developerKey, username, password);
            return new YouTubeRequest(settings);
        }

        public PicasaService initializePicasa()
        {
            PicasaService service = new PicasaService("BlueTube");
            service.setUserCredentials(username, password);
            return service;
        }

        public void DealWithRequest()
        {
            while (ol.IsListening)
            {
                //receive files via bluetooth
                try
                {
                    ObexListenerContext olc = ol.GetContext();
                    ObexListenerRequest olr = olc.Request;

                    string filename = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) +
                                        "\\" +
                                        DateTime.Now.ToString("dd-MM-yy HHmm") +
                                        " " +
                                        Uri.UnescapeDataString(olr.RawUrl.TrimStart(new char[] { '/' }));
                    //remove newline
                    filename = filename.Substring(0, filename.Length - 1);
                    Console.Write("filetype: " + filename.Substring(filename.Length-3,3));
                    olr.WriteFile(filename);
                    
                    //add filename to fileList
                    DoFileListUpdate(filename);

                    if(filename.Substring(filename.Length-3,3).Equals("jpg") ||
                        filename.Substring(filename.Length-3,3).Equals("JPG") ||
                        filename.Substring(filename.Length-3,3).Equals("png") ||
                        filename.Substring(filename.Length-3,3).Equals("PNG") ||
                        filename.Substring(filename.Length-3,3).Equals("GIF") ||
                        filename.Substring(filename.Length-3,3).Equals("gif")) 
                    {
                        //image upload to picasa
                        
                        Uri postUri = new Uri(PicasaQuery.CreatePicasaUri(username, albumid));

                        System.IO.FileInfo fileInfo = new System.IO.FileInfo(filename);
                        System.IO.FileStream fileStream = fileInfo.OpenRead();
                        PicasaEntry entry = (PicasaEntry) picasaService.Insert(postUri, fileStream, "image/jpeg", filename);
                        fileStream.Close();
                        //image properties
                        entry.Title.Text = "Gogbot09: " + olr.RawUrl.TrimStart(new char[] { '/' });
                        entry.Summary.Text = "User content for GOGBOT 2009 festival, provided by bluetooth device: " +
                                                olr.UserHostAddress +
                                                " on: " +
                                                DateTime.Now.ToString("dd-MM-yy HHmm");
                        entry.Media.Keywords.Value = "gogbot, GOGBOT09, gogbot2009, bluetube";
                        //entry.Location = new GeoRssWhere();
                        //entry.Location.Latitude = 37;
                        //entry.Location.Longitude = -122;

                        PicasaEntry updatedEntry = (PicasaEntry) entry.Update();
                        Console.WriteLine("Photo: " + olr.RawUrl.TrimStart(new char[] { '/' }) + " uploaded.");

                    } else {
                        //upload video to youtube
                        Video newVideo = new Video();

                        newVideo.Title = "Gogbot09: " + olr.RawUrl.TrimStart(new char[] { '/' });
                        newVideo.Tags.Add(new MediaCategory("People", YouTubeNameTable.CategorySchema));
                        newVideo.Keywords = "gogbot, GOGBOT09, gogbot2009, bluetube";
                        newVideo.Description = "User content for GOGBOT 2009 festival, provided by bluetooth device: " +
                                                olr.UserHostAddress +
                                                " on: " +
                                                DateTime.Now.ToString("dd-MM-yy HHmm");
                        newVideo.YouTubeEntry.Private = false;
                        newVideo.Tags.Add(new MediaCategory("uploaded_through_bluetube",
                          YouTubeNameTable.DeveloperTagSchema));

                        //newVideo.YouTubeEntry.Location = new GeoRssWhere(37, -122);
                        // alternatively, you could just specify a descriptive string
                        newVideo.YouTubeEntry.setYouTubeExtension("location", "Vrije Universiteit, Amsterdam");

                        newVideo.YouTubeEntry.MediaSource = new MediaFileSource(filename, "video/3gpp");
                        Video video = request.Upload(newVideo);

                        //Check status
                        if (video.IsDraft)
                        {
                            Console.WriteLine("Video is not live.");
                            string stateName = video.Status.Name;
                            if (stateName == "processing")
                            {
                                Console.WriteLine("Video is still being processed.");
                            }
                            else if (stateName == "rejected")
                            {
                                Console.Write("Video has been rejected because: ");
                                Console.WriteLine(video.Status.Value);
                                Console.Write("For help visit: ");
                                Console.WriteLine(video.Status.Help);
                            }
                            else if (stateName == "failed")
                            {
                                Console.Write("Video failed uploading because:");
                                Console.WriteLine(video.Status.Value);
                                Console.Write("For help visit: ");
                                Console.WriteLine(video.Status.Help);
                            }
                        }
                     }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private delegate void UpdateStatusDelegate(String message);

        private void DoFileListUpdate(String message)
        {
            if (this.InvokeRequired)
            {
                // we were called on a worker thread
                // marshal the call to the user interface thread
                this.Invoke(new UpdateStatusDelegate(DoFileListUpdate),
                            new object[] { message });
                return;
            }

            // this code can only be reached
            // by the user interface thread
            this.fileBox.Items.Add(message);
        } 

        /*public void searchForDevices()
        {
            while (true)
            {
                BluetoothDeviceInfo[] array = bc.DiscoverDevices();
                foreach (BluetoothDeviceInfo bti in array)
                {
                    fileList.Items.Add(bti.DeviceName);
                }
                fileList.Clear();
            }

        }*/

        public BlueTube()
        {
            InitializeComponent();
            request = initializeYoutube();
            picasaService = initializePicasa();

            ol = new ObexListener(ObexTransport.Bluetooth);
            bc = new InTheHand.Net.Sockets.BluetoothClient();
            try
            {
                ol.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                System.Windows.Forms.MessageBox.Show("Bluetube failed to start, enable a compatible bluetooth adapter");
                Environment.Exit(0);
            }

            Thread t1 = new Thread(new System.Threading.ThreadStart(DealWithRequest));
            t1.Start();

            //BluetoothDeviceInfo[] array = bc.DiscoverDevices();
            //deviceList.Items.Add(""+array.Length);
            //for (int i = 0; i < array.Length; i++)
            //{
            //    deviceList.Items.Add(array[i].DeviceName);
            //}
        }

        //by tag
        //feel free to change number of items, by there is a limit of 50, I believe. 
        //If you want to retreive more, you have to do a loop (retrieve 1-50, then 51 to 100, etc)
        private void button1_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            Uri videoEntryUrl =
              new Uri("http://gdata.youtube.com/feeds/api/videos/ADos_xW4_J0");
            Video video = request.Retrieve<Video>(videoEntryUrl);

            Console.Write("Title: " + video.Title);
            Console.Write(video.Description);
        }
    }
}
