using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Graphics.Printing;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PhotoBooth
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainPage : Page
    {
        //CameraCaptureUI captureUI = new CameraCaptureUI();
        MediaCapture _mediaCapture;
        bool _isPreviewing = false;
        DisplayRequest _displayRequest;
        int picstaken = 0;
        int totpics = 3;
        Image activepicture;
        List<SoftwareBitmap> images = new List<SoftwareBitmap>();
        bool complete = false;
        int[] countdown = { 10, 6, 6, 6 };
        //int[] countdown = { 3,3,3,3 };
        private DispatcherTimer dispatchTimer = new DispatcherTimer();
        Windows.Storage.StorageFolder savePicturesFolder = KnownFolders.PicturesLibrary; 


        public MainPage()
        {
            this.InitializeComponent();
            //captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            //captureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);
            activepicture = (Image)this.FindName("image" + picstaken.ToString());
            System.Diagnostics.Debug.WriteLine("pb" + DateTime.Now.ToString("ssmmHHddMMyy") + ".bmp");
            dispatchTimer.Tick += dispatchTimer_Tick;
            dispatchTimer.Interval = new TimeSpan(0, 0, 1);
            Application.Current.Suspending += Application_Suspending;
        }

        private async Task StartPreviewAsync()
        {
            try
            {

                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;

                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed. {0}", ex.Message);
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            if (complete == false)
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();
                //_mediaCapture.Failed += Medi
                await CapturePhotoAsync();
            }

        }

        private async Task CapturePhotoAsync()
        {
            var lowlagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
            var capturedPhoto = await lowlagCapture.CaptureAsync();
            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;
            var softwareBitmapSource = new SoftwareBitmapSource();
            await softwareBitmapSource.SetBitmapAsync(SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied) );
            await lowlagCapture.FinishAsync();

            
            _isPreviewing = false;
            /*var filename = "pb" + DateTime.Now.ToString("ssmmHHddMMyy") + ".bmp";
            var file = await savePicturesFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await _mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);

            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            SoftwareBitmapSource softwareBitmapSource = new SoftwareBitmapSource();
            await softwareBitmapSource.SetBitmapAsync(SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied));*/

            activepicture.Source = softwareBitmapSource;
            images.Add(softwareBitmap);
            await CleanupCameraAsync();

            if (picstaken < totpics)
            {
                picstaken++;
                activepicture = (Image)this.FindName("image" + picstaken.ToString());
                await StartPreviewAsync();
            }
            else
            {
                complete = true;
                StartButton.Visibility = Visibility.Visible;
                dispatchTimer.Stop();
                mergePhotos();
            }
        }

        private async Task CleanupCameraAsync()
        {
            if (_mediaCapture != null)
            {
               /* if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                    _isPreviewing = false;
                }*/

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (_displayRequest != null)
                    {
                        _displayRequest.RequestRelease();
                    }

                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });
            }

        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
        }

        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }

        private async void startPreview_Click(object sender, RoutedEventArgs e)
        {
            if(_isPreviewing == false)
            {
                await StartPreviewAsync();
            }
            else
            {
                await CleanupCameraAsync();
            }
            
        }

        private void hide_StartButton(object sender, RoutedEventArgs e)
        {
            if(complete == true)
            {
                _isPreviewing = false;
                complete = false;
                picstaken = 0;
                for (int i = 0; i <= totpics;i++)
                {
                    activepicture = (Image)this.FindName("image" + i);
                    activepicture.Source = null;
                }
                activepicture = (Image)this.FindName("image0");
                countdown = new int[]{ 12, 6, 6, 6 };
                dispatchTimer.Start();
            }
            //sButton.Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            //sButton.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            StartButton.Visibility = Visibility.Collapsed;
            System.Diagnostics.Debug.WriteLine("trying to hide");
            startPreview_Click(sender, e);
            dispatchTimer.Start();
        }

        private async void dispatchTimer_Tick(object sender, object e)
        {
            if (countdown[picstaken] > 1)
            {
                cdBox.Visibility = Visibility.Visible;
                countdown[picstaken] = countdown[picstaken] - 1;
                if(countdown[picstaken] - 1 < 10)
                    cdBox.Text = Convert.ToString((countdown[picstaken] - 1));
                else
                    cdBox.Text = " " + Convert.ToString((countdown[picstaken] - 1));
            }
            else
            {
                cdBox.Visibility = Visibility.Collapsed;
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();
                //_mediaCapture.Failed += Medi
                await CapturePhotoAsync();
            }
        }

        private async void mergePhotos()
        {
            if(complete == true)
            {
                try
                {
                    WriteableBitmap outputImage = new WriteableBitmap(images[0].PixelWidth * 2, images[0].PixelHeight * (totpics + 1));
                    WriteableBitmap inputImage = new WriteableBitmap(images[0].PixelWidth, images[0].PixelHeight);
                    const int BYTES_PER_PIXEL = 4;
                    SoftwareBitmap outputImage2 = new SoftwareBitmap(images[0].BitmapPixelFormat, images[0].PixelWidth * 2, images[0].PixelHeight * (totpics + 1));
                    int owidth = outputImage.PixelWidth;
                    int oheight = outputImage.PixelHeight;
                    int iwidth = images[0].PixelWidth;
                    int iheight = images[0].PixelHeight;
                    IBuffer inputbuffer = inputImage.PixelBuffer;
                    int bytesperpixel = 4;
                    int istride = iwidth * bytesperpixel;
                    int ostride = owidth * bytesperpixel;
                    byte[] imgdata = new byte[owidth * oheight * bytesperpixel];
                    byte[] inputdata;
                    int row = 0;
                    for (int i = 0; i < totpics+1; i++)
                    {
                        images[i].CopyToBuffer(inputbuffer);
                        inputdata = inputbuffer.ToArray();
                        while(row < (iheight * (i+1)))
                        {
                            for(int col = 0; col < iwidth;col++)
                            {
                                // BGRA
                                //set left image
                                imgdata[row * ostride + col * 4 + 0] = inputdata[(row - (iheight * i)) * istride + col * 4 + 0];
                                imgdata[row * ostride + col * 4 + 1] = inputdata[(row - (iheight * i)) * istride + col * 4 + 1];
                                imgdata[row * ostride + col * 4 + 2] = inputdata[(row - (iheight * i)) * istride + col * 4 + 2];
                                imgdata[row * ostride + col * 4 + 3] = inputdata[(row - (iheight * i)) * istride + col * 4 + 3];
                                //set right image
                                imgdata[row * ostride + (col + iwidth) * 4 + 0] = inputdata[(row - (iheight * i)) * istride + col * 4 + 0];
                                imgdata[row * ostride + (col + iwidth) * 4 + 1] = inputdata[(row - (iheight * i)) * istride + col * 4 + 1];
                                imgdata[row * ostride + (col + iwidth) * 4 + 2] = inputdata[(row - (iheight * i)) * istride + col * 4 + 2];
                                imgdata[row * ostride + (col + iwidth) * 4 + 3] = inputdata[(row - (iheight * i)) * istride + col * 4 + 3];
                            }
                            row++;
                        }
                    }
                    
                    outputImage2.CopyFromBuffer(imgdata.AsBuffer());
                    StorageFile imageFile = await savePicturesFolder.CreateFileAsync("pb" + DateTime.Now.ToString("ssmmHHddMMyy") + ".bmp", CreationCollisionOption.ReplaceExisting);
                    System.Diagnostics.Debug.WriteLine("pb"+ DateTime.Now.ToString("ssmmHHddMMyy") + ".bmp");
                    SaveSoftwareBitmapToFile(outputImage2, imageFile);
                    
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error Merging Photos");
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
        {
            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);

                // Set additional encoding parameters, if needed
                encoder.BitmapTransform.ScaledWidth = (uint)softwareBitmap.PixelWidth;
                encoder.BitmapTransform.ScaledHeight = (uint)softwareBitmap.PixelHeight;
                //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                encoder.BitmapTransform.Rotation = 0;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    switch (err.HResult)
                    {
                        case unchecked((int)0x88982F81): //WINCODEC_ERR_UNSUPPORTEDOPERATION
                                                         // If the encoder does not support writing a thumbnail, then try again
                                                         // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw err;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                }

                PrintBitmapFromFile(outputFile);
            }
        }

        private async void PrintBitmapFromFile(StorageFile imgfile)
        {

            
        }
    }
}
