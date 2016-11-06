using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.Devices.Gpio;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NADYSpeechApiProj
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Speech Commands You Can Say
        //==================        
        // hi SMSM
        // Turn On/Off Bedroom Light
        // Turn On/Off kITCHEN Light
        // Fan On/Off       
        //================== 
        #endregion

        #region 

        // Grammer File
        private const string SRGS_FILE = "Grammar\\grammar.xml";
        // Speech Recognizer
        private SpeechRecognizer recognizer;
        // Tag TARGET
        private const string TAG_TARGET = "location";
        // Tag CMD
        private const string TAG_CMD = "cmd";
        // Tag Device
        private const string TAG_DEVICE = "device";

        #endregion

        #region BedRoom
        private const int BedRoomLED_PINNumber = 5;
        private GpioPin BedRoomLED_GpioPin;
        private GpioPinValue BedRoomLED_GpioPinValue;
        private DispatcherTimer bedRoomTimer;
        #endregion

        #region kITCHEN
        private const int kITCHENLED_PINNumber = 12;
        private GpioPin kITCHENLED_GpioPin;
        private GpioPinValue kITCHENLED_GpioPinValue;
        private DispatcherTimer kITCHENTimer;
        #endregion

        #region FAN
        //private const int FAN_PINNumber = 5;
        //private GpioPin FAN_GpioPin;
        //private GpioPinValue FAN_GpioPinValue;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            Unloaded += MainPage_Unloaded;

            // Initialize Recognizer
            initializeSpeechRecognizer();
            //-----            
            bedRoomTimer = new DispatcherTimer();
            bedRoomTimer.Interval = TimeSpan.FromMilliseconds(500);
            bedRoomTimer.Tick += BedRoomTimer_Tick;
            //--------
            //-----            
            kITCHENTimer = new DispatcherTimer();
            kITCHENTimer.Interval = TimeSpan.FromMilliseconds(500);
            kITCHENTimer.Tick += KITCHENTimer_Tick;
            //--------
        }


        // Initialize Speech Recognizer and start async recognition
        private async void initializeSpeechRecognizer()
        {
            // Initialize recognizer
            recognizer = new SpeechRecognizer();

            #region Create Events
            // Set event handlers
            recognizer.StateChanged += RecognizerStateChanged;
            recognizer.ContinuousRecognitionSession.ResultGenerated += RecognizerResultGenerated;
            #endregion

            #region Load Grammer

            // Load Grammer file constraint
            string fileName = String.Format(SRGS_FILE);
            StorageFile grammarContentFile = await Package.Current.InstalledLocation.GetFileAsync(fileName);

            SpeechRecognitionGrammarFileConstraint grammarConstraint = new SpeechRecognitionGrammarFileConstraint(grammarContentFile);

            // Add to grammer constraint
            recognizer.Constraints.Add(grammarConstraint);
            #endregion

            #region Compile grammer
            SpeechRecognitionCompilationResult compilationResult = await recognizer.CompileConstraintsAsync();
            Debug.WriteLine("Status: " + compilationResult.Status.ToString());

            // If successful, display the recognition result.
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("Result: " + compilationResult.ToString());

                await recognizer.ContinuousRecognitionSession.StartAsync();
            }
            else
            {
                Debug.WriteLine("Status: " + compilationResult.Status);
            }
            #endregion
        }

        // Recognizer generated results
        private void RecognizerResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // Output debug strings
            Debug.WriteLine(args.Result.Status);
            Debug.WriteLine(args.Result.Text);
            int count = args.Result.SemanticInterpretation.Properties.Count;
            Debug.WriteLine("Count: " + count);
            Debug.WriteLine("Tag: " + args.Result.Constraint.Tag);

            // Check for different tags and initialize the variables
            String location = args.Result.SemanticInterpretation.Properties.ContainsKey(TAG_TARGET) ?
                            args.Result.SemanticInterpretation.Properties[TAG_TARGET][0].ToString() :
                            "";

            String cmd = args.Result.SemanticInterpretation.Properties.ContainsKey(TAG_CMD) ?
                            args.Result.SemanticInterpretation.Properties[TAG_CMD][0].ToString() :
                            "";

            String device = args.Result.SemanticInterpretation.Properties.ContainsKey(TAG_DEVICE) ?
                            args.Result.SemanticInterpretation.Properties[TAG_DEVICE][0].ToString() :
                            "";

            Debug.WriteLine("Target: " + location + ", Command: " + cmd + ", Device: " + device);

            #region
            switch (device)
            {
                case "hiActivationCMD"://Activate SMSM                    
                    SaySomthing("hiActivationCMD", "On");
                    break;

                case "fan":
                    FanControl(cmd);
                    break;

                case "LIGHT":
                    LightControl(cmd, location);
                    break;

                default:
                    break;
            }
            #endregion


        }

        // Recognizer state changed
        private void RecognizerStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine("Speech recognizer state: " + args.State.ToString());
        }

        private void SaySomthing(string myDevice, string State, int speechCharacterVoice = 0)
        {
            if (myDevice == "hiActivationCMD")
                PlayVoice($"Hi Naady What can i do for you");//12
            else
                PlayVoice($"Ok NADY {myDevice}  {State}", speechCharacterVoice);
            Debug.WriteLine($"OKOKOK -> ===== {myDevice} --- {State} =======");
        }

        private void FanControl(string command)
        {
            if (command == "ON")
            {

            }
            else if (command == "Off")
            {

            }

            SaySomthing("Fan", command);
        }

        private async void LightControl(string command, string target)
        {
            if (target == "Bedroom")
            {
                //=========================== 
                if (command == "ON")
                {
                    InitBedRoomGPIO();
                    if (BedRoomLED_GpioPin != null)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                       {
                           bedRoomTimer.Start();
                       }
                     );

                    }
                }
                else if (command == "OFF")
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        bedRoomTimer.Stop();
                        if (BedRoomLED_GpioPinValue == GpioPinValue.Low)
                        {
                            BedRoomLED_GpioPinValue = GpioPinValue.High;
                            BedRoomLED_GpioPin.Write(BedRoomLED_GpioPinValue);
                            //LED.Fill = redBrush;
                        }
                    }
                    );
                }
                //===========================
            }
            else if (target == "kitchen")
            {
                //=========================== 
                if (command == "ON")
                {
                    InitKITCHENGPIO();
                    if (kITCHENLED_GpioPin != null)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            kITCHENTimer.Start();
                        }
                     );

                    }
                }
                else if (command == "OFF")
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        kITCHENTimer.Stop();
                        if (kITCHENLED_GpioPinValue == GpioPinValue.Low)
                        {
                            kITCHENLED_GpioPinValue = GpioPinValue.High;
                            kITCHENLED_GpioPin.Write(kITCHENLED_GpioPinValue);
                            //LED.Fill = redBrush;
                        }
                    }
                    );
                }
                //===========================
            }

            SaySomthing($"{target} light", command);
        }

        private void InitBedRoomGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                BedRoomLED_GpioPin = null;
                //GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            BedRoomLED_GpioPin = gpio.OpenPin(BedRoomLED_PINNumber);
            BedRoomLED_GpioPinValue = GpioPinValue.High;
            BedRoomLED_GpioPin.Write(BedRoomLED_GpioPinValue);
            BedRoomLED_GpioPin.SetDriveMode(GpioPinDriveMode.Output);

            //GpioStatus.Text = "GPIO pin initialized correctly.";

        }

        private void InitKITCHENGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                kITCHENLED_GpioPin = null;
                //GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            kITCHENLED_GpioPin = gpio.OpenPin(kITCHENLED_PINNumber);
            kITCHENLED_GpioPinValue = GpioPinValue.High;
            kITCHENLED_GpioPin.Write(kITCHENLED_GpioPinValue);
            kITCHENLED_GpioPin.SetDriveMode(GpioPinDriveMode.Output);

            //GpioStatus.Text = "GPIO pin initialized correctly.";

        }

        private void BedRoomTimer_Tick(object sender, object e)
        {
            if (BedRoomLED_GpioPinValue == GpioPinValue.High)
            {
                BedRoomLED_GpioPinValue = GpioPinValue.Low;
                BedRoomLED_GpioPin.Write(BedRoomLED_GpioPinValue);
                //LED.Fill = redBrush;
            }
            else
            {
                BedRoomLED_GpioPinValue = GpioPinValue.High;
                BedRoomLED_GpioPin.Write(BedRoomLED_GpioPinValue);
                //LED.Fill = grayBrush;
            }
        }

        private void KITCHENTimer_Tick(object sender, object e)
        {
            if (kITCHENLED_GpioPinValue == GpioPinValue.High)
            {
                kITCHENLED_GpioPinValue = GpioPinValue.Low;
                kITCHENLED_GpioPin.Write(kITCHENLED_GpioPinValue);
                //LED.Fill = redBrush;
            }
            else
            {
                kITCHENLED_GpioPinValue = GpioPinValue.High;
                kITCHENLED_GpioPin.Write(kITCHENLED_GpioPinValue);
                //LED.Fill = grayBrush;
            }
        }

        // Release resources, stop recognizer etc...
        private async void MainPage_Unloaded(object sender, object args)
        {
            // Stop recognizing
            await recognizer.ContinuousRecognitionSession.StopAsync();

            // Release pins           
            recognizer.Dispose();
            recognizer = null;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //Listinstalledvoices();
            //PlayVoice(textBox1.Text);
            PlayVoice(textBox1.Text, int.Parse(textBox2.Text));
        }

        private void Listinstalledvoices()
        {
            //https://www.microsoft.com/en-us/download/details.aspx?id=27224
            //https://www.microsoft.com/en-us/download/details.aspx?id=27225
            //http://superuser.com/questions/590779/how-to-install-more-voices-to-windows-speech
            //http://www.laptopmag.com/articles/change-cortanas-voice-windows-10
            foreach (VoiceInformation voiceInfo in SpeechSynthesizer.AllVoices)
            {
                textBox1.Text += $"{voiceInfo.DisplayName} __ ";
                Debug.WriteLine(voiceInfo.DisplayName);
            }
        }

        private async void PlayVoice(string tTS, int voice = 0)
        {
            //await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
            //=====================
            // The media object for controlling and playing audio.           
            // The object for controlling the speech synthesis engine (voice).          
            SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
            //speechSynthesizer.Voice = SpeechSynthesizer.DefaultVoice;
            speechSynthesizer.Voice = SpeechSynthesizer.AllVoices[voice];//0,4,8,12


            // Generate the audio stream from plain text.
            SpeechSynthesisStream spokenStream = await speechSynthesizer.SynthesizeTextToStreamAsync(tTS);
            //================
            await mediaElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                  {
                      mediaElement.SetSource(spokenStream, spokenStream.ContentType);
                      mediaElement.Play();
                  }));
            //===============

            //===============
            // Send the stream to the media object.
            //mediaElement.SetSource(spokenStream, spokenStream.ContentType);
            // mediaElement.Play();
            //=====================
            //});
        }
    }
}