using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AutoStock.Mobile;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient = new HttpClient();
    private string? _customOperationName;
    public MainPage()
    {
        InitializeComponent();

        
        TotalLabel.Text = 0.ToString("C2", new CultureInfo("tr-TR"));
    }

    private void OnAddOperationClicked(object? sender, EventArgs e)
    {
        var selectedOperation = OperationPicker.SelectedItem?.ToString();

        var operationName = selectedOperation == "Diğer"
            ? _customOperationName
            : selectedOperation;

        var quantityText = OperationQuantityEntry.Text?.Trim();
        var priceText = OperationPriceEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(operationName))
        {
            ResultLabel.Text = "Lütfen işlem seç veya elle işlem adı gir.";
            return;
        }

        if (!int.TryParse(quantityText, out var quantity) || quantity <= 0)
        {
            ResultLabel.Text = "Lütfen geçerli bir adet gir.";
            return;
        }

        if (!decimal.TryParse(
                NormalizePrice(priceText),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var unitPrice) || unitPrice <= 0)
        {
            ResultLabel.Text = "Lütfen geçerli bir fiyat gir.";
            return;
        }

        AddOperationRow(operationName, quantity, unitPrice);

        OperationPicker.SelectedIndex = -1;
        _customOperationName = null;
        OperationQuantityEntry.Text = "1";
        OperationPriceEntry.Text = string.Empty;
        ResultLabel.Text = string.Empty;

        CalculateTotal();
    }

    private void AddOperationRow(string name, int quantity, decimal unitPrice)
    {
        var totalPrice = quantity * unitPrice;

        var nameLabel = new Label
        {
            Text = name,
            TextColor = Color.FromArgb("#111827"),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        };

        var detailLabel = new Label
        {
            Text = $"{quantity} adet × {unitPrice.ToString("C2", new CultureInfo("tr-TR"))}",
            TextColor = Color.FromArgb("#6B7280"),
            FontSize = 12
        };

        var priceLabel = new Label
        {
            Text = totalPrice.ToString("C2", new CultureInfo("tr-TR")),
            TextColor = Color.FromArgb("#111827"),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalOptions = LayoutOptions.Center
        };

        var rowGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 110 }
            },
            ColumnSpacing = 10
        };

        rowGrid.Add(new Label
        {
            Text = "🛠️",
            FontSize = 18,
            VerticalOptions = LayoutOptions.Center
        }, 0, 0);

        rowGrid.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                nameLabel,
                detailLabel
            }
        }, 1, 0);

        rowGrid.Add(priceLabel, 2, 0);

        var rowContent = new Border
        {
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            Stroke = Color.FromArgb("#E5E7EB"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 14),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(16)
            },
            Content = rowGrid
        };

        rowContent.BindingContext = new OperationItem
        {
            Name = name,
            Quantity = quantity,
            UnitPrice = unitPrice
        };

        SwipeView? swipeView = null;

        swipeView = new SwipeView
        {
            RightItems =
            {
                new SwipeItem
                {
                    Text = "Sil",
                    BackgroundColor = Color.FromArgb("#EF4444"),
                    Command = new Command(() =>
                    {
                        if (swipeView != null)
                        {
                            OperationsContainer.Remove(swipeView);
                            CalculateTotal();
                        }
                    })
                }
            },
            Content = rowContent
        };

        OperationsContainer.Add(swipeView);
    }
    private async void OnOperationPickerChanged(object? sender, EventArgs e)
    {
        if (OperationPicker.SelectedItem?.ToString() == "Diğer")
        {
            var result = await DisplayPromptAsync(
                "Diğer işlem",
                "İşlem adını gir:",
                "Kaydet",
                "İptal",
                "Örn: Turbo hortumu değişimi");

            if (!string.IsNullOrWhiteSpace(result))
            {
                _customOperationName = result.Trim();
            }
            else
            {
                OperationPicker.SelectedIndex = -1;
                _customOperationName = null;
            }
        }
    }
    private void CalculateTotal()
    {
        decimal total = 0;

        foreach (var child in OperationsContainer.Children)
        {
            if (child is SwipeView swipeView &&
                swipeView.Content is Border border &&
                border.BindingContext is OperationItem item)
            {
                total += item.Quantity * item.UnitPrice;
            }
        }

        TotalLabel.Text = total.ToString("C2", new CultureInfo("tr-TR"));
    }

    private async void OnCreatePdfClicked(object? sender, EventArgs e)
    {
        var operations = new List<object>();

        foreach (var child in OperationsContainer.Children)
        {
            if (child is SwipeView swipeView &&
                swipeView.Content is Border border &&
                border.BindingContext is OperationItem item)
            {
                operations.Add(new
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.Quantity * item.UnitPrice
                });
            }
        }

        var data = new
        {
            CustomerName = CustomerNameEntry.Text,
            CustomerPhone = CustomerPhoneEntry.Text,
            CustomerEmail = CustomerEmailEntry.Text,

            Plate = PlateEntry.Text,
            Brand = BrandEntry.Text,
            Model = ModelEntry.Text,
            ModelYear = ModelYearEntry.Text,

            Operations = operations
        };

        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            ResultLabel.Text = "PDF oluşturuluyor...";

            var response = await _httpClient.PostAsync(
                "http://10.0.2.2:5122/api/ServicePdfs",
                content);

            if (response.IsSuccessStatusCode)
            {
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();

                var filePath = System.IO.Path.Combine(
                    FileSystem.AppDataDirectory,
                    $"servis-formu-{DateTime.Now:yyyyMMdd-HHmmss}.pdf"
                );

                await File.WriteAllBytesAsync(filePath, pdfBytes);

                ResultLabel.Text = "PDF oluşturuldu.";

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Servix Servis Formu",
                    File = new ShareFile(filePath)
                });

                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ResultLabel.Text = $"API hata verdi: {response.StatusCode} - {errorText}";
            }
        }
        catch (Exception ex)
        {
            ResultLabel.Text = ex.Message;
        }
    }

    private static string NormalizePrice(string? text)
    {
        return text?
            .Replace("₺", "")
            .Replace(" ", "")
            .Replace(".", "")
            .Replace(",", ".")
            ?? "";
    }

    private class OperationItem
    {
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}