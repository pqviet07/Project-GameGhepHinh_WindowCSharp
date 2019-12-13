using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GameXepHinh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //width = 250 / size;
            //height = 250 / size;
        }

        const int startX = 50;
        const int startY = 50;
        int width = 50;
        int height = 50;
        int size = 3;
        const int thickness = 2;
        private Image[,] matrixImage;
        private int[,] tagMatrix;
        private Point emptyCell;
        private string ImagePath;
        private Point lastMove;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        int timeRemaining = 0;
        bool wasStartGame = false;
        bool ImageLoaded = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            timerLabel.Visibility = Visibility.Hidden;

            //matrixImage = new Image[size, size];
            //tagMatrix = new int[size, size];
            //emptyCell.X = size - 1;
            //emptyCell.Y = size - 1;
            //lastMove.X = -1;
            //lastMove.Y = -1;

        }

        bool isDragging = false;
        Image selectedBitmap = null;
        Point lastPosition;


        private void CropImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);

            int i = ((int)position.Y - startY) / height;
            int j = ((int)position.X - startX) / width;
            //this.Title = $"{position.X} - {position.Y}, a[{i}][{j}]";
            Tuple<int, int> p = getCoordinateFromTag((int)selectedBitmap.Tag);
            if (timeRemaining == 0 || !wasStartGame || !ImageLoaded)
            {
                Canvas.SetLeft(selectedBitmap, startX + p.Item2 * (width + thickness));
                Canvas.SetTop(selectedBitmap, startY + p.Item1 * (height + thickness));
                isDragging = false;
                return;
            }
            if (!((int)position.Y > startY + size * height + (size - 1) * thickness ||
                (int)position.X > startX + size * width + (size - 1) * thickness) && tagMatrix[i, j] == -1 && isValidMove(p.Item1, p.Item2))
            {
                Canvas.SetLeft(selectedBitmap, startX + j * (width + thickness));
                Canvas.SetTop(selectedBitmap, startY + i * (height + thickness));

                emptyCell.X = p.Item1;
                emptyCell.Y = p.Item2;

                tagMatrix[i, j] = tagMatrix[p.Item1, p.Item2];
                tagMatrix[p.Item1, p.Item2] = -1;

                matrixImage[i, j] = matrixImage[p.Item1, p.Item2];
                matrixImage[p.Item1, p.Item2] = null;

                behaviorAfterWon();
            }
            else
            {
                Canvas.SetLeft(selectedBitmap, startX + p.Item2 * (width + thickness));
                Canvas.SetTop(selectedBitmap, startY + p.Item1 * (height + thickness));
            }
            isDragging = false;
        }
        // Kiểm tra di chuyển từ Ô [i,j] đến ô trống có hợp lệ không
        // Hợp lệ return true, ngược lại return false
        private bool isValidMove(int i, int j)
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int k = 0; k < dx.Length; ++k)
            {
                if (isValidCell((int)emptyCell.X + dx[k], (int)emptyCell.Y + dy[k]))
                {
                    if ((i == (int)emptyCell.X + dx[k]) && (j == (int)emptyCell.Y + dy[k]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Kiểm trả ô [i,j] có hợp lệ không
        private bool isValidCell(int i, int j)
        {
            if (i < 0 || i >= size || j < 0 || j >= size)
            {
                return false;
            }
            return true;
        }
        private void CropImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            selectedBitmap = sender as Image;
            //var idx = canvas.Children.IndexOf(selectedBitmap);
            //Tuple<int, int> p = selectedBitmap.Tag as Tuple<int, int>;
            //this.Title = $"{p.Item1} - {p.Item2}";
            selectedBitmap.Focus();
            isDragging = true;
            lastPosition = e.GetPosition(selectedBitmap);

        }

        private void CropImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging)
            {
                var img = (Image)sender;
                Point temp = e.GetPosition(this);
                Canvas.SetLeft(selectedBitmap, temp.X - lastPosition.X);
                Canvas.SetTop(selectedBitmap, temp.Y - lastPosition.Y);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int index = 0;

            if (btn.Name.Equals("rightBtn"))
            {
                index = 0;
            }
            else if (btn.Name.Equals("leftBtn"))
            {
                index = 1;
            }
            else if (btn.Name.Equals("upBtn"))
            {
                index = 2;
            }
            else if (btn.Name.Equals("downBtn"))
            {
                index = 3;
            }
            MakeMove(index);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int index = 0;
            if (e.Key == System.Windows.Input.Key.Up)
            {
                index = 2;
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                index = 3;
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                index = 1;
            }
            else if (e.Key == System.Windows.Input.Key.Right)
            {
                index = 0;
            }

            MakeMove(index);
        }

        // ----------------------------------------------------
        private void MakeMove(int index)
        {
            if (timeRemaining == 0 || !wasStartGame)
            {
                return;
            }
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { -1, 1, 0, 0 };
            // thứ tự duyệt: right left up down

            int i = (int)emptyCell.X;
            int j = (int)emptyCell.Y;

            if (!isValidCell(i + dx[index], j + dy[index]))
            {
                return;
            }

            Canvas.SetLeft(matrixImage[i + dx[index], j + dy[index]], startX + j * (width + thickness));
            Canvas.SetTop(matrixImage[i + dx[index], j + dy[index]], startY + i * (height + thickness));

            emptyCell.X = i + dx[index];
            emptyCell.Y = j + dy[index];

            tagMatrix[i, j] = tagMatrix[i + dx[index], j + dy[index]];
            tagMatrix[i + dx[index], j + dy[index]] = -1;

            matrixImage[i, j] = matrixImage[i + dx[index], j + dy[index]];
            matrixImage[i + dx[index], j + dy[index]] = null;

            behaviorAfterWon();
        }

        // Lấy ra tọa độ của Image trong ma trận tagMatrix có Tag là tg
        private Tuple<int, int> getCoordinateFromTag(int tg)
        {
            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    if (tagMatrix[i, j] == tg)
                    {
                        return new Tuple<int, int>(i, j);
                    }
                }
            }
            return new Tuple<int, int>(-1, -1);
        }

        // Kiểm tra các hình đã đc sắp xếp đúng thứ tự chưa
        // Nếu đã sắp xếp đúng thứ tự return true; ngược lại return false;
        private bool checkWin()
        {
            int count = -1;
            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    ++count;
                    if (tagMatrix[i, j] == -1 && (i != size - 1 || j != size - 1))
                    {
                        return false;
                    }
                    else if (tagMatrix[i, j] != -1)
                    {
                        if (count != (int)matrixImage[i, j].Tag)
                        {
                            return false;
                        }
                    }
                }
            }
            cropButton.IsEnabled = true;
            return true;
        }
        private void behaviorAfterWon()
        {
            if (checkWin())
            {
                stopTimer();
                MessageBox.Show("WIN ROAI");
                resetTimer();

            }
        }

        private void shuffle(int num)
        {
            if (ImageLoaded)
            {
                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { -1, 1, 0, 0 };
                Random rand = new Random();
                for (int i = 0; i < num; ++i)
                {
                    int x = (int)emptyCell.X;
                    int y = (int)emptyCell.Y;

                    int randNum = rand.Next() % 4;
                    while (!isValidCell(x + dx[randNum], y + dy[randNum]) || ((x + dx[randNum] == (int)lastMove.X) &&
                        (y + dy[randNum] == (int)lastMove.Y)))
                    {
                        randNum = rand.Next() % 4;
                    }
                    moveImage(x + dx[randNum], y + dy[randNum]);

                }
            }
        }
        private bool moveImage(int i, int j)
        {

            if (isValidMove(i, j))
            {

                matrixImage[(int)emptyCell.X, (int)emptyCell.Y] = matrixImage[i, j];
                matrixImage[i, j] = null;

                tagMatrix[(int)emptyCell.X, (int)emptyCell.Y] = tagMatrix[i, j];
                tagMatrix[i, j] = -1;


                marginImage((int)emptyCell.X, (int)emptyCell.Y);


                lastMove.X = emptyCell.X;
                lastMove.Y = emptyCell.Y;

                emptyCell.X = i;
                emptyCell.Y = j;

                //MessageBox.Show("hhh");
                return true;
            }
            return false;
        }
        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            var screen = new OpenFileDialog();
            screen.Filter = "Text Files (.txt)|*.txt";
            screen.InitialDirectory = Environment.CurrentDirectory;
            if (screen.ShowDialog() == true)
            {
                canvas.Children.RemoveRange(1, size * size);
                resetTimer();
                var filename = screen.FileName;
                var reader = new StreamReader(filename);
                ImagePath = reader.ReadLine();
                size = int.Parse(reader.ReadLine());


                Image[,] tempMatrixImage = LoadImage(ImagePath);

                for (int i = 0; i < size; ++i)
                {
                    var tokens = reader.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.None);
                    for (int j = 0; j < tokens.Length; ++j)
                    {
                        tagMatrix[i, j] = int.Parse(tokens[j]);

                        if (tagMatrix[i, j] != -1)
                        {
                            int x = (tagMatrix[i, j] + 1) % size == 0 ? (tagMatrix[i, j] + 1) / size - 1 : (tagMatrix[i, j] + 1) / size;
                            int y = (tagMatrix[i, j] + 1) % size == 0 ? size - 1 : (tagMatrix[i, j] + 1) % size - 1;
                            matrixImage[i, j] = tempMatrixImage[x, y];

                            marginImage(i, j);
                        }
                        else
                        {
                            matrixImage[i, j] = null;
                            emptyCell.X = i;
                            emptyCell.Y = j;
                        }
                    }
                }
            }
        }
        private void marginImage(int i, int j)
        {
            Canvas.SetLeft(matrixImage[i, j], startX + j * (width + thickness));
            Canvas.SetTop(matrixImage[i, j], startY + i * (height + thickness));
        }
        BitmapImage source;
        private Image[,] LoadImage(string ImgPath)
        {
            source = new BitmapImage(new Uri(ImgPath));
           

            clearAll();

            previewImage.Source = source;

            realImageWidth = source.PixelWidth;
            realImageHeight = source.PixelHeight;
        
            //cat thanh 9 manh

            int tag = 0;
            Image[,] tempMatrixImage = new Image[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (!((i == size - 1) && (j == size - 1)))
                    {
                        var rect = new Int32Rect((50) * (j + 5), (50) * (i + 2), (50), (50));
                        var cropBitmap = new CroppedBitmap(source, rect);

                        var cropImage = new Image();
                        cropImage.Stretch = Stretch.Fill;
                        cropImage.Width = width;
                        cropImage.Height = height;
                        cropImage.Source = cropBitmap;
                        canvas.Children.Add(cropImage);

                        cropImage.MouseDown += CropImage_MouseDown;
                        cropImage.MouseMove += CropImage_MouseMove;
                        cropImage.MouseUp += CropImage_MouseUp;
                        cropImage.Tag = tag;

                        tagMatrix[i, j] = tag++;
                        tempMatrixImage[i, j] = cropImage;
                    }
                    else
                    {
                        tagMatrix[i, j] = -1;
                    }
                }
            }

            ImageLoaded = true;
            return tempMatrixImage;
        }


        // Chọn vị trí crop
        double realImageWidth, realImageHeight;
        double lengthRectCrop, leftRectCrop, topRectCrop;
        double newLeftRectCrop, newTopRectCrop;
        double widthHinhHienThi, heightHinhHienThi;
        double leftHinhHienThi, topHinhHienThi, rightHinhHienThi, bottomHinhHienThi;
        bool isFinshedCropping = false;
        double tiLe;

        private void cropButton_Click(object sender, RoutedEventArgs e)
        {
            if (previewImage.Source == null)
            {
                MessageBox.Show("Chưa chọn hình");
                return;
            }
            if (isFinshedCropping)
            {
                canvas.Children.RemoveRange(1, size * size);                             
                emptyCell.X = size - 1;
                emptyCell.Y = size - 1;
                lastMove.X = -1;
                lastMove.Y = -1;                
                resetTimer();

                int tag = 0;
                Image[,] tempMatrixImage = new Image[size, size];
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        if (!((i == size - 1) && (j == size - 1)))
                        {
                            var rect = new Int32Rect((int)((leftRectCrop - leftHinhHienThi + j * (lengthRectCrop / size)) * (tiLe)), (int)((topRectCrop - topHinhHienThi + i * (lengthRectCrop / size)) * (tiLe)), (int)((lengthRectCrop / size) * tiLe), (int)((lengthRectCrop / size) * tiLe));
                            var cropBitmap = new CroppedBitmap(source, rect);
                            
                            var cropImage = new Image();
                            cropImage.Stretch = Stretch.Fill;
                            cropImage.Width = 50;
                            cropImage.Height = 50;
                            cropImage.Source = cropBitmap;
                            canvas.Children.Add(cropImage);

                            cropImage.MouseDown += CropImage_MouseDown;
                            cropImage.MouseMove += CropImage_MouseMove;
                            cropImage.MouseUp += CropImage_MouseUp;
                            cropImage.Tag = tag;

                            Canvas.SetLeft(cropImage, startX + j * (50 + thickness));
                            Canvas.SetTop(cropImage, startY + i * (50 + thickness));

                            tagMatrix[i, j] = tag++;
                            matrixImage[i, j] = cropImage;
                        }
                        else
                        {
                            tagMatrix[i, j] = -1;
                        }
                    }
                }

                cropButton.Content = "Crop";
                isFinshedCropping = false;
                eraseRectangle();
                return;
            }

            isFinshedCropping = true;
            eraseRectangle();
            cropButton.Content = "OK";

            // vẽ hình chữ nhật để crop
            if (realImageWidth / realImageHeight <= (5.0 / 3.0))
            {
                //tiLe = realImageHeight / 300;
                widthHinhHienThi = 300 * realImageWidth / realImageHeight;
                heightHinhHienThi = 300;
                lengthRectCrop = widthHinhHienThi <= heightHinhHienThi ? widthHinhHienThi : heightHinhHienThi;
                leftRectCrop = 450 + (500 - widthHinhHienThi) / 2;
                topRectCrop = 45;
                leftHinhHienThi = leftRectCrop;
                topHinhHienThi = topRectCrop;
                rightHinhHienThi = leftHinhHienThi + widthHinhHienThi;
                bottomHinhHienThi = topHinhHienThi + heightHinhHienThi;
                if (lengthRectCrop > 250)
                    lengthRectCrop = 250;

                drawRectangle(lengthRectCrop, lengthRectCrop, leftRectCrop , topRectCrop);
            }
            else
            {
                //tiLe = realImageWidth / 500;
                widthHinhHienThi = 500;
                heightHinhHienThi = 500 * realImageHeight / realImageWidth;
                lengthRectCrop = widthHinhHienThi <= heightHinhHienThi ? widthHinhHienThi : heightHinhHienThi;
                leftRectCrop = 450;
                topRectCrop = 45 + (300 - heightHinhHienThi) / 2;
                leftHinhHienThi = leftRectCrop;
                topHinhHienThi = topRectCrop;
                rightHinhHienThi = leftHinhHienThi + widthHinhHienThi;
                bottomHinhHienThi = topHinhHienThi + heightHinhHienThi;
                if (lengthRectCrop > 250)
                    lengthRectCrop = 250;
                
                drawRectangle(lengthRectCrop, lengthRectCrop , leftRectCrop, topRectCrop);
            }
            tiLe = Math.Sqrt((realImageWidth * realImageHeight) / (widthHinhHienThi * heightHinhHienThi));
           
        }



        public void drawRectangle(double width, double height, double left, double top)
        {
            System.Windows.Shapes.Rectangle rect;
            rect = new System.Windows.Shapes.Rectangle();
            rect.Stroke = new SolidColorBrush(Colors.Black);
            rect.StrokeThickness = 2;
            rect.Fill = new SolidColorBrush(Colors.Transparent);
            rect.Width = width;
            rect.Height = height;
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            rect.Name = "CropRectangle";
            this.RegisterName(rect.Name, rect);
            rect.MouseLeftButtonDown += rect_MouseLeftButtonDown;
            rect.MouseLeftButtonUp += rect_MouseLeftButtonUp;
            rect.MouseMove += rect_MouseMove;
            canvas.Children.Add(rect);
        }
        bool isMovingRectangle = false;
        Point firstPoint;
        Point currentPoint;

        private void rect_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isMovingRectangle)
                return;
            currentPoint = e.GetPosition(this);
            eraseRectangle();
            newLeftRectCrop = leftRectCrop + (currentPoint.X - firstPoint.X);
            newTopRectCrop = topRectCrop + (currentPoint.Y - firstPoint.Y);
            if (newLeftRectCrop < leftHinhHienThi)
                newLeftRectCrop = leftHinhHienThi;
            if (newLeftRectCrop + lengthRectCrop > rightHinhHienThi)
                newLeftRectCrop = rightHinhHienThi - lengthRectCrop;
            if (newTopRectCrop < topHinhHienThi)
                newTopRectCrop = topHinhHienThi;
            if (newTopRectCrop + lengthRectCrop > bottomHinhHienThi)
                newTopRectCrop = bottomHinhHienThi - lengthRectCrop;

            drawRectangle(lengthRectCrop, lengthRectCrop, newLeftRectCrop, newTopRectCrop);
        }

        private void rect_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isMovingRectangle = false;
            firstPoint = currentPoint;
            leftRectCrop = newLeftRectCrop;
            topRectCrop = newTopRectCrop;
            isFinshedCropping = true;
        }

        private void rect_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isMovingRectangle)
            {
                isMovingRectangle = true;
                firstPoint = e.GetPosition(this);
                currentPoint = firstPoint;
            }
        }
        public void eraseRectangle()
        {
            UIElement child = canvas.FindName("CropRectangle") as UIElement;
            if (child == null)
            {
                return;
            }
            this.UnregisterName("CropRectangle");
            canvas.Children.Remove(child);

        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ImageLoaded)
            {
                const string filename = "save_game.txt";
                var writer = new StreamWriter(filename);
                writer.WriteLine(ImagePath);
                writer.WriteLine(size);
                for (int i = 0; i < size; ++i)
                {
                    for (int j = 0; j < size; ++j)
                    {
                        writer.Write(tagMatrix[i, j]);
                        if (j != size - 1)
                        {
                            writer.Write(" ");
                        }
                    }
                    writer.WriteLine("");
                }
                writer.Close();
                MessageBox.Show("Game saved!");
            }
            else
            {
                MessageBox.Show("404 Image not found!");
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            clearAll();
        }

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            shuffle(75);
        }


        // đếm ngược
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (ImageLoaded)
            {
                if (wasStartGame == false)
                {
                    wasStartGame = true;
                }
                startButton.IsEnabled = false;
                timerLabel.Visibility = Visibility.Visible;
                timeRemaining = 30; // giây
                timerLabel.Content = timeRemaining.ToString();
                dispatcherTimer.Start();

                cropButton.Content = "Crop";
                cropButton.IsEnabled = false;
                isFinshedCropping = false;
                eraseRectangle();
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            timeRemaining--;
            timerLabel.Content = timeRemaining.ToString();
            if (timeRemaining == 0)
            {
                startButton.IsEnabled = true;
                timerLabel.Visibility = Visibility.Hidden;
                dispatcherTimer.Stop();

                cropButton.IsEnabled = true;
                MessageBox.Show("Đã hết thời gian, You close");
            }
        }

        private void NewgameBtn_Click(object sender, RoutedEventArgs e)
        {           
            var screen = new OpenFileDialog();
            screen.Filter = "Images Files|*.png;*.jpg;*.jpeg;*.bmp";
            screen.InitialDirectory = Environment.CurrentDirectory;
            if (screen.ShowDialog() == true)
            {
                
                clearAll();
                size = sizeBox.SelectedIndex + 3;
                if (size < 3)
                {
                    MessageBox.Show("Hãy chọn size");
                    return;
                }
                ImagePath = screen.FileName;
                matrixImage = LoadImage(ImagePath);
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        if (!((i == size - 1) && (j == size - 1)))
                        {
                            marginImage(i, j);
                        }
                    }
                }
            }
        }

        private void resetTimer()
        {
            dispatcherTimer.Stop();
            startButton.IsEnabled = true;
            timerLabel.Content = "30";
            timeRemaining = 30;
            wasStartGame = false;
        }
        private void stopTimer()
        {
            dispatcherTimer.Stop();
        }
        private void clearAll()
        {
            canvas.Children.RemoveRange(1, size * size);
            previewImage.Source = null;
            matrixImage = new Image[size, size];
            tagMatrix = new int[size, size];
            emptyCell.X = size - 1;
            emptyCell.Y = size - 1;
            lastMove.X = -1;
            lastMove.Y = -1;
            ImageLoaded = false;
            resetTimer();

            eraseRectangle();
            cropButton.Content = "Crop";
            cropButton.IsEnabled = true;
            isFinshedCropping = false;
        }
       
    }
}
