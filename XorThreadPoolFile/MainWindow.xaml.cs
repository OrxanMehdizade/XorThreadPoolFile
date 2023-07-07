using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace XorThreadPoolFile
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cts;
        private byte[] encryptionKey;
        private bool isEncrypting;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenDestinationButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                destinationTextBox.Text = openFileDialog.FileName;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = destinationTextBox.Text;
            if (!string.IsNullOrEmpty(filePath))
            {
                encryptionKey = GenerateEncryptionKey();
                isEncrypting = encryptRadioButton.IsChecked ?? false;

                cts = new CancellationTokenSource();

                await Task.Run(() =>
                {
                    if (isEncrypting)
                    {
                        ThreadPool.QueueUserWorkItem(state => EncryptFile(filePath));
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(state => DecryptFile(filePath));
                    }
                });
            }
            else
            {
                MessageBox.Show("Bir File seçin.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }

        private void EncryptFile(object state)
        {
            string filePath = (string)state;
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                long fileSize = fileBytes.Length;
                long processedBytes = 0;

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                {
                    for (int i = 0; i < fileSize; i++)
                    {
                        if (cts.IsCancellationRequested)
                        {
                            fileStream.Seek(0, SeekOrigin.Begin);
                            fileStream.Write(fileBytes, 0, fileBytes.Length);
                            fileStream.SetLength(fileBytes.Length);
                            fileStream.Flush();
                            break;
                        }

                        fileStream.Seek(i, SeekOrigin.Begin);
                        fileStream.WriteByte((byte)(fileBytes[i] ^ encryptionKey[i % encryptionKey.Length]));

                        processedBytes++;
                        UpdateProgressBar(processedBytes, fileSize);
                        Thread.Sleep(50); 
                    }
                }

                // İlerleme çubuğunu sıfırla
                UpdateProgressBar(0, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message);
            }
        }

        private void DecryptFile(object state)
        {
            string filePath = (string)state;
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                long fileSize = fileBytes.Length;
                long processedBytes = 0;

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                {
                    for (int i = 0; i < fileSize; i++)
                    {
                        if (cts.IsCancellationRequested)
                        {
                            // İşlem iptal edildiğinde dosyayı önceki durumuna geri döndür
                            fileStream.Seek(0, SeekOrigin.Begin);
                            fileStream.Write(fileBytes, 0, fileBytes.Length);
                            fileStream.SetLength(fileBytes.Length);
                            fileStream.Flush();
                            break;
                        }

                        fileStream.Seek(i, SeekOrigin.Begin);
                        fileStream.WriteByte((byte)(fileBytes[i] ^ encryptionKey[i % encryptionKey.Length]));

                        processedBytes++;
                        UpdateProgressBar(processedBytes, fileSize);
                        Thread.Sleep(50); // İşlemi yavaşlatmak için bir bekleme süresi ekleyin
                    }
                }

                if (!cts.IsCancellationRequested)
                {
                    MessageBox.Show("Şifre çözme işlemi başarıyla tamamlandı.");
                }
                else
                {
                    MessageBox.Show("Şifre çözme işlemi iptal edildi.");
                }

                // İlerleme çubuğunu sıfırla
                UpdateProgressBar(0, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message);
            }
        }

        private void UpdateProgressBar(long processedBytes, long fileSize)
        {
            Dispatcher.Invoke(() =>
            {
                double progress = (double)processedBytes / fileSize;
                progressBar.Value = progress * 100;

                // Geriye doğru ilerlemeyi göstermek için ProgressBar'ın değerini ayarlayın
                if (cts != null && cts.IsCancellationRequested)
                {
                    double reverseProgress = (double)(fileSize - processedBytes) / fileSize;
                    progressBar.Value = progressBar.Maximum - (reverseProgress * 100);
                }
            });
        }

        private byte[] GenerateEncryptionKey()
        {
            // Rastgele bir şifreleme anahtarı oluştur
            byte[] key = new byte[16];
            new Random().NextBytes(key);
            return key;
        }
    }
}