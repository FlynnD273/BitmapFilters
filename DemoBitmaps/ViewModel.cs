using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DemoBitmaps
{
    class ViewModel : ViewModelBase
    {
        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => _UpdateProperty(ref _fileName, value);
        }

        private WriteableBitmap _wBitmap;
        public WriteableBitmap WBitmap
        {
            get => _wBitmap;
            set => _UpdateProperty(ref _wBitmap, value);
        }

        //All ICommands bound to different buttons
        public ICommand PromptFileNameCommand { get; }
        public List<ICommand> Commands = new List<ICommand>();
        public List<string> CommandNames = new List<string>();
        public ICommand ReloadImageCommand { get; }
        public ICommand ExportImageCommand { get; }

        private int promptNum = 0;
        private Window filterWin = new FilterOptionsWindow();

        //variables that I want to access in other methods
        private byte blue;
        private byte green;
        private byte red;
        private int width;
        private int height;

        /// <summary>
        /// The algorithm used to filter the image. This gets swapped out for different Actions
        /// </summary>
        private Action<int, int> FilterAlg;

        private int buttonsPerRow = 6;

        public ViewModel()
        {
            //Init button commands
            PromptFileNameCommand = new DelegateCommand(_PromptFileName);
            ReloadImageCommand = new DelegateCommand(_ReloadImage);
            ExportImageCommand = new DelegateCommand(_ExportImage);

            //Add all the filters with names
            _AddCom(_MaxValGray, "Max Value Grayscale");
            _AddCom(_AvgValGray, "Average Value Grayscale");
            _AddCom(_MinValGray, "Min Value Grayscale");

            _AddCom(_MaxValOnly, "Max Value Only");
            _AddCom(_MinValOnly, "Min Value Only");

            _AddCom(_MaxValInt, "Max Value Intensified");
            _AddCom(_MaxValDet, "Max Value Detensified");

            _AddCom(_MinValInt, "Min Value Intensified");
            _AddCom(_MinValDet, "Min Value Detensified");

            _AddCom(_MiddleValInt, "Middle Value Intensified");
            _AddCom(_MiddleValDet, "Middle Value Detensified");

            _AddCom(_Gradient, "Color Gradient");
            _AddCom(_XGradient, "Horizontal Gradient");

            _AddCom(_RedOnly, "Red Only");
            _AddCom(_GreenOnly, "Green Only");
            _AddCom(_BlueOnly, "Blue Only");
        }

        /// <summary>
        /// Method to populate the "command" and "command names" lists
        /// </summary>
        /// <param name="command">The action to add to the "commands" list</param>
        /// <param name="name">The text that appears on the button</param>
        private void _AddCom(Action command, string name)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            Commands.Add(new DelegateCommand(command));
            CommandNames.Add(name);
        }

        /// <summary>
        /// Convert to grayscale using the highest value channel
        /// </summary>
        private void _MaxValGray()
        {
            FilterAlg = (int x, int y) =>
            {
                byte maxVal = Math.Max(Math.Max(red, blue), green);

                blue = maxVal;
                green = maxVal;
                red = maxVal;
            };
            _FilterImage();
        }

        /// <summary>
        /// Convert to grayscale using the average value from all three channels
        /// </summary>
        private void _AvgValGray()
        {
            FilterAlg = (int x, int y) =>
            {
                byte avgVal = (byte)Math.Round((decimal)(blue+green+red)/3);

                blue = avgVal;
                green = avgVal;
                red = avgVal;
            };
            _FilterImage();
        }

        /// <summary>
        /// Convert to grayscale using the lowest value channel
        /// </summary>
        private void _MinValGray()
        {
            FilterAlg = (int x, int y) =>
            {
                byte minVal = Math.Min(Math.Min(red, blue), green);

                blue = minVal;
                green = minVal;
                red = minVal;
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the highest value channel to 255, all other channels are set to 0
        /// </summary>
        private void _MaxValOnly()
        {
            FilterAlg = (int x, int y) =>
            {
                byte maxVal = Math.Max(Math.Max(red, blue), green);

                if (maxVal == blue)
                {
                    blue = 255;
                    green = 0;
                    red = 0;
                }
                else if(maxVal == green)
                {
                    green = 255;
                    blue = 0;
                    red = 0;
                }
                else
                {
                    red = 255;
                    blue = 0;
                    green = 0;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the lowest value channel to 255, all other channels are set to 0
        /// </summary>
        private void _MinValOnly()
        {
            FilterAlg = (int x, int y) =>
            {
                byte minVal = Math.Min(Math.Min(red, blue), green);

                if (minVal == blue)
                {
                    blue = 255;
                    green = 0;
                    red = 0;
                }
                else if (minVal == green)
                {
                    green = 255;
                    blue = 0;
                    red = 0;
                }
                else
                {
                    red = 255;
                    blue = 0;
                    green = 0;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the highest value channel to 255, all other channels are untouched
        /// </summary>
        private void _MaxValInt()
        {
            FilterAlg = (int x, int y) =>
            {
                byte maxVal = Math.Max(Math.Max(red, blue), green);

                if (maxVal == blue)
                {
                    blue = 255;
                }
                else if (maxVal == green)
                {
                    green = 255;
                }
                else
                {
                    red = 255;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the lowest value channel to 0, all other channels are untouched
        /// </summary>
        private void _MinValDet()
        {
            FilterAlg = (int x, int y) =>
            {
                byte minVal = Math.Min(Math.Min(red, blue), green);

                if (minVal == blue)
                {
                    blue = 0;
                }
                else if (minVal == green)
                {
                    green = 0;
                }
                else
                {
                    red = 0;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the highest value channel to 0, all other channels are untouched
        /// </summary>
        private void _MaxValDet()
        {
            FilterAlg = (int x, int y) =>
            {
                byte maxVal = Math.Max(Math.Max(red, blue), green);

                if (maxVal == blue)
                {
                    blue = 0;
                }
                else if (maxVal == green)
                {
                    green = 0;
                }
                else
                {
                    red = 0;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the lowest value channel to 0, all other channels are untouched
        /// </summary>
        private void _MinValInt()
        {
            FilterAlg = (int x, int y) =>
            {
                byte minVal = Math.Min(Math.Min(red, blue), green);

                if (minVal == blue)
                {
                    blue = 255;
                }
                else if (minVal == green)
                {
                    green = 255;
                }
                else
                {
                    red = 255;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the middle value channel to 0, all other channels are untouched
        /// </summary>
        private void _MiddleValDet()
        {
            FilterAlg = (int x, int y) =>
            {
                byte maxVal = Math.Max(Math.Max(red, blue), green);
                byte minVal = Math.Min(Math.Min(red, blue), green);

                if (maxVal != blue && minVal != blue)
                {
                    blue = 0;
                }
                else if (maxVal != green && minVal != green)
                {
                    green = 0;
                }
                else
                {
                    red = 0;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the middle value channel to 255, all other channels are untouched
        /// </summary>
        private void _MiddleValInt()
        {
            FilterAlg = (int x, int y) =>
            {
                byte maxVal = Math.Max(Math.Max(red, blue), green);
                byte minVal = Math.Min(Math.Min(red, blue), green);

                if (maxVal != blue && minVal != blue)
                {
                    blue = 255;
                }
                else if (maxVal != green && minVal != green)
                {
                    green = 255;
                }
                else
                {
                    red = 255;
                }
            };
            _FilterImage();
        }

        /// <summary>
        /// Set the blue channel to the x position of the pixel, and set the red channel to the y position of the channel
        /// </summary>
        private void _Gradient()
        {
            FilterAlg = (int x, int y) =>
            {
                blue = (byte)(Math.Round((decimal)x / width * 255));
                red = (byte)(Math.Round((decimal)y / height * 255));
            };
            _FilterImage();
        }

        /// <summary>
        /// Adjust the brightness of each pixel based on its x position
        /// </summary>
        private void _XGradient()
        {
            FilterAlg = (int x, int y) =>
            {
                int newVal = blue + (int)Math.Round((decimal)x / width * 255) - 127;
                blue = (byte)newVal.Clamp(0,255);
                newVal = green + (int)Math.Round((decimal)x / width * 255) - 127;
                green = (byte)newVal.Clamp(0, 255);
                newVal = red + (int)Math.Round((decimal)x / width * 255) - 127;
                red = (byte)newVal.Clamp(0, 255);
            };
            _FilterImage();
        }

        /// <summary>
        /// Set blue and green pixels to 0
        /// </summary>
        private void _RedOnly()
        {
            FilterAlg = (int x, int y) =>
            {
                blue = 0;
                green = 0;
            };
            _FilterImage();
        }

        /// <summary>
        /// Set red and green pixels to 0
        /// </summary>
        private void _BlueOnly()
        {
            FilterAlg = (int x, int y) =>
            {
                red = 0;
                green = 0;
            };
            _FilterImage();
        }

        /// <summary>
        /// Set blue and red pixels to 0
        /// </summary>
        private void _GreenOnly()
        {
            FilterAlg = (int x, int y) =>
            {
                blue = 0;
                red = 0;
            };
            _FilterImage();
        }

        /// <summary>
        /// The method that changes the values of each pixel
        /// </summary>
        /// <param name="win">The settings dialog for the selected filter</param>
        private unsafe void _FilterImage(Window win = null)
        {
            //The start of a settings dialog popup window for more variety in filters
            if (win == null)
                win = filterWin;

            //general info for the bitmap
            width = WBitmap.PixelWidth;
            height = WBitmap.PixelHeight;
            int stride = WBitmap.BackBufferStride;
            int bytesPerPixel = (WBitmap.Format.BitsPerPixel + 7) / 8;

            //Create a new array to contain the pixel data of the bitmap
            byte[] pixels = new byte[width * height * 4];
            WBitmap.CopyPixels(pixels, stride, 0);
            

            //Iterate through all pixels
            for (int i = 0; i < pixels.Length; i+=bytesPerPixel)
            {
                blue = pixels[i];
                green = pixels[i+1];
                red = pixels[i+2];

                //Run the Action FilterAlg, passing an x and y coordinate
                FilterAlg((i/ bytesPerPixel) % width, (int)Math.Floor((decimal)(i/ bytesPerPixel) / width));

                //Blue
                pixels[i] = blue;
                //Green
                pixels[i+1] = green;
                //Red
                pixels[i+2] = red;
                //Alpha
                pixels[i+3] = 255;
            }

            //Copy pixel information to back to the WriteableBitmap
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            WBitmap.WritePixels(rect, pixels, stride, 0);
        }

        /// <summary>
        /// Writes the filtered WriteableBitmap to a file
        /// </summary>
        private void _ExportImage()
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                if (sfd.ShowDialog() == true)
                {
                    string fileName = Path.GetFileNameWithoutExtension(sfd.FileName);

                    FileStream stream = new FileStream(Path.GetDirectoryName(sfd.FileName) + "\\" + fileName + ".jpg", FileMode.Create);
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(WBitmap));
                    encoder.Save(stream);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to create image. {e.GetType().Name}: {e.Message}", "File error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Loads an Bitmap from the selected source
        /// </summary>
        private void _ReloadImage()
        {
            try
            {
                BitmapSource bSource = new BitmapImage(new Uri(FileName));
                if (bSource.Format != PixelFormats.Bgra32)
                    bSource = new FormatConvertedBitmap(bSource, PixelFormats.Bgra32, null, 0);
                WBitmap = new WriteableBitmap(bSource);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load image. {e.GetType().Name}: {e.Message}", "File error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// dynamically add the filter buttons from the "commands" list
        /// </summary>
        private void _InitButtons()
        {
            //The current row that buttons are being added to
            int rows = 0;

            Grid btnGrid = (Grid)Application.Current.MainWindow.FindName("ButtonGrid");//ButtonGrid

            //Add all of the buttons that are a multiple of ButtonsPerRow
            for (rows = 0; rows < Math.Floor((decimal)Commands.Count / buttonsPerRow); rows++)
            {
                RowDefinition rowDef = new RowDefinition();
                rowDef.Height = GridLength.Auto;
                btnGrid.RowDefinitions.Add(rowDef);

                //Add ButtonsPerRow number of buttons to each row
                for (int i = 0; i < buttonsPerRow; i++)
                {
                    //Create a new column
                    if (rows == 0)
                    {
                        ColumnDefinition colDef = new ColumnDefinition();
                        colDef.Width = new GridLength(buttonsPerRow, GridUnitType.Auto);
                        btnGrid.ColumnDefinitions.Add(colDef);
                    }

                    //Set up the button control
                    Button btn = new Button();
                    Grid.SetColumn(btn, i);
                    Grid.SetRow(btn, rows);
                    btn.Content = CommandNames[i + buttonsPerRow * rows];
                    btn.Command = Commands[i + buttonsPerRow * rows];
                    btn.Margin = new Thickness(2);

                    //Add the button to the current row in the button grid
                    btnGrid.Children.Add(btn);
                }
            }

            //Add the remainder of the buttons to the last row
            if (Commands.Count % buttonsPerRow != 0)
            {
                RowDefinition rowDef = new RowDefinition();
                rowDef.Height = GridLength.Auto;
                btnGrid.RowDefinitions.Add(rowDef);

                for (int i = 0; i < Commands.Count % buttonsPerRow; i++)
                {
                    //Set up the button control
                    Button btn = new Button();
                    Grid.SetColumn(btn, i);
                    Grid.SetRow(btn, rows);
                    btn.Content = CommandNames[i+buttonsPerRow*rows];
                    btn.Command = Commands[i+buttonsPerRow*rows];
                    btn.Margin = new Thickness(2);

                    btnGrid.Children.Add(btn);
                }
            }
        }

        /// <summary>
        /// Uses OpenFileDialog to prompt the user for an image, then loads the image
        /// </summary>
        private void _PromptFileName()
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == true)
            {
                //Store the image path in a variable
                FileName = ofd.FileName;
                //Load image
                _ReloadImage();
                //Add buttons after the first image is loaded to avoid nullException errors
                if(promptNum++ == 0)
                    _InitButtons();
            }
        }
    }
}
