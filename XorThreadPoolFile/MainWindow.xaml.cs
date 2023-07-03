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

                if (isEncrypting)
                {
                    await Task.Run(() => EncryptFile(filePath)); // Şifreleme işlemini arka plan iş parçacığına taşı
                }
                else
                {
                    await Task.Run(() => DecryptFile(filePath)); // Şifre çözme işlemini arka plan iş parçacığına taşı
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir dosya seçin.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }

        private async Task EncryptFile(string filePath)
        {
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
                        await UpdateProgressBar(processedBytes, fileSize);
                    }
                }

                if (!cts.IsCancellationRequested)
                {
                    MessageBox.Show("Şifreleme işlemi başarıyla tamamlandı.");
                }
                else
                {
                    MessageBox.Show("Şifreleme işlemi iptal edildi.");
                }

                // İlerleme çubuğunu sıfırla
                await UpdateProgressBar(0, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message);
            }
        }

        private async Task DecryptFile(string filePath)
        {
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
                        await UpdateProgressBar(processedBytes, fileSize);
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
                await UpdateProgressBar(0, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message);
            }
        }

        private async Task UpdateProgressBar(long processedBytes, long fileSize)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                double progress = (double)processedBytes / fileSize;
                progressBar.Value = progress * 100;
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