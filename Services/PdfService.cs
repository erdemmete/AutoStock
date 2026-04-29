using AutoStock.Repositories.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services
{
    public class PdfService : IPdfService
    {
        public byte[] CreateServicePdf(CreateServicePdfRequest request)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var total = request.Items.Sum(x => x.Price * x.Quantity);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    page.Header()
                        .Text("Servis İşlem Formu")
                        .FontSize(22)
                        .Bold();

                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        column.Item().Text($"Müşteri: {request.CustomerName}");
                        column.Item().Text($"Telefon: {request.CustomerPhone}");
                        column.Item().Text($"Plaka: {request.PlateNumber}");
                        column.Item().Text($"Araç: {request.VehicleBrand} {request.VehicleModel}");

                        if (!string.IsNullOrWhiteSpace(request.Note))
                            column.Item().Text($"Not: {request.Note}");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("İşlem").Bold();
                                header.Cell().Text("Adet").Bold();
                                header.Cell().Text("Birim Fiyat").Bold();
                                header.Cell().Text("Toplam").Bold();
                            });

                            foreach (var item in request.Items)
                            {
                                table.Cell().Text(item.Description);
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text($"{item.Price:N2} TL");
                                table.Cell().Text($"{(item.Price * item.Quantity):N2} TL");
                            }
                        });

                        column.Item()
                            .AlignRight()
                            .Text($"Genel Toplam: {total:N2} TL")
                            .FontSize(16)
                            .Bold();
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });

            return pdf.GeneratePdf();
        }
    }
}
