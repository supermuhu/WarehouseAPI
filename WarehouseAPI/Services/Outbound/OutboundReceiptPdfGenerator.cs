using System;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WarehouseAPI.ModelView.Outbound;

namespace WarehouseAPI.Services.Outbound
{
    public static class OutboundReceiptPdfGenerator
    {
        public static byte[] Generate(OutboundReceiptPrintViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var document = new OutboundReceiptDocument(model);
            return document.GeneratePdf();
        }

        private class OutboundReceiptDocument : IDocument
        {
            private readonly OutboundReceiptPrintViewModel _model;

            public OutboundReceiptDocument(OutboundReceiptPrintViewModel model)
            {
                _model = model;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, _model));
                    page.Content().Element(c => ComposeContent(c, _model));
                    page.Footer().Element(c => ComposeFooter(c, _model));
                });
            }

            private void ComposeHeader(IContainer container, OutboundReceiptPrintViewModel model)
            {
                container.Column(stack =>
                {
                    stack.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(text =>
                            {
                                text.Span("Đơn vị: ").SemiBold().FontSize(10);
                                text.Span(model.WarehouseName ?? string.Empty).FontSize(10);
                            });

                            left.Item().Text(text =>
                            {
                                text.Span("Bộ phận: ").SemiBold().FontSize(10);
                                text.Span("........................................").FontSize(10);
                            });
                        });

                        row.ConstantItem(220).Column(right =>
                        {
                            right.Item().AlignRight().Text(text =>
                            {
                                text.Span("Mẫu số 02 - VT").Bold().FontSize(10);
                            });
                            right.Item().AlignRight().Text(text =>
                            {
                                text.Span("(Ban hành theo Thông tư số 133/2016/TT-BTC").FontSize(8);
                            });
                            right.Item().AlignRight().Text(text =>
                            {
                                text.Span("ngày 26/08/2016 của Bộ Tài chính)").FontSize(8);
                            });
                        });
                    });

                    stack.Item().AlignCenter().Text(text =>
                    {
                        text.Span("PHIẾU XUẤT KHO").FontSize(16).Bold();
                    });

                    var date = model.OutboundDate ?? DateTime.Now;
                    stack.Item().AlignCenter().Text(text =>
                    {
                        text.Span($"Ngày {date:dd} tháng {date:MM} năm {date:yyyy}").FontSize(10);
                    });

                    stack.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Số: ").SemiBold().FontSize(10);
                            text.Span(model.ReceiptNumber ?? string.Empty).FontSize(10);
                        });

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Nợ: ").SemiBold().FontSize(10);
                            text.Span("................   ").FontSize(10);
                            text.Span("Có: ").SemiBold().FontSize(10);
                            text.Span("................").FontSize(10);
                        });
                    });

                    stack.Item().Text(text =>
                    {
                        text.Span("- Họ và tên người nhận hàng: ").FontSize(10);
                        text.Span(model.CustomerName ?? string.Empty).FontSize(10);
                        text.Span("    Địa chỉ (bộ phận): ........................................").FontSize(10);
                    });

                    stack.Item().Text(text =>
                    {
                        text.Span("- Lý do xuất kho: ").FontSize(10);
                        if (!string.IsNullOrWhiteSpace(model.Notes))
                        {
                            text.Span(model.Notes).FontSize(10);
                        }
                        else
                        {
                            text.Span("...............................................................")
                                .FontSize(10);
                        }
                    });

                    stack.Item().Text(text =>
                    {
                        text.Span("- Xuất tại kho (ngăn lô): ").FontSize(10);
                        text.Span(model.WarehouseName ?? string.Empty).FontSize(10);
                        text.Span("    Địa điểm: ........................................").FontSize(10);
                    });

                    stack.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });
            }

            private void ComposeContent(IContainer container, OutboundReceiptPrintViewModel model)
            {
                var viCulture = new CultureInfo("vi-VN");
                var items = (model.Items ?? Enumerable.Empty<OutboundReceiptPrintItemViewModel>()).ToList();

                var totalQuantity = items.Sum(i => (decimal)i.Quantity);
                var totalAmount = items.Sum(i => i.TotalAmount ?? 0m);

                container.Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);    // STT
                            columns.RelativeColumn(4);     // Tên hàng hóa
                            columns.RelativeColumn(2);     // Mã số
                            columns.RelativeColumn(1.5f);  // Đơn vị tính
                            columns.RelativeColumn(2);     // Số lượng yêu cầu
                            columns.RelativeColumn(2);     // Số lượng thực xuất
                            columns.RelativeColumn(2);     // Đơn giá
                            columns.RelativeColumn(2);     // Thành tiền
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("STT");
                            header.Cell().Element(HeaderCell).Text(
                                "Tên, nhãn hiệu, quy cách,\nphẩm chất vật tư, dụng cụ sản phẩm, hàng hóa");
                            header.Cell().Element(HeaderCell).Text("Mã số");
                            header.Cell().Element(HeaderCell).Text("Đơn vị tính");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Số lượng yêu cầu");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Số lượng thực xuất");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền");
                        });

                        var index = 1;
                        foreach (var item in items)
                        {
                            var qtyText = item.Quantity.ToString("N0", viCulture);
                            var unitPriceText = item.UnitPrice.HasValue
                                ? item.UnitPrice.Value.ToString("#,0.##", viCulture)
                                : string.Empty;
                            var amountText = item.TotalAmount.HasValue
                                ? item.TotalAmount.Value.ToString("#,0.##", viCulture)
                                : string.Empty;

                            table.Cell().Element(Cell).Text(index.ToString());

                            table.Cell().Element(Cell).Text(text =>
                            {
                                var name = item.ProductName ?? item.ItemName ?? string.Empty;
                                text.Line(name);
                                if (!string.IsNullOrWhiteSpace(item.BatchNumber))
                                {
                                    text.Line($"Lô: {item.BatchNumber}");
                                }
                                if (!string.IsNullOrWhiteSpace(item.QrCode))
                                {
                                    text.Line($"Mã QR: {item.QrCode}");
                                }
                            });

                            table.Cell().Element(Cell).Text(item.ProductCode ?? string.Empty);
                            table.Cell().Element(Cell).Text(item.Unit ?? string.Empty);
                            table.Cell().Element(Cell).AlignRight().Text(qtyText);
                            table.Cell().Element(Cell).AlignRight().Text(qtyText);
                            table.Cell().Element(Cell).AlignRight().Text(unitPriceText);
                            table.Cell().Element(Cell).AlignRight().Text(amountText);

                            index++;
                        }

                        if (items.Any())
                        {
                            table.Cell().Element(FooterCell).Text(string.Empty);
                            table.Cell().Element(FooterCell).Text("Cộng");
                            table.Cell().Element(FooterCell).Text(string.Empty);
                            table.Cell().Element(FooterCell).Text(string.Empty);
                            table.Cell().Element(FooterCell).AlignRight()
                                .Text(totalQuantity.ToString("N0", viCulture));
                            table.Cell().Element(FooterCell).AlignRight()
                                .Text(totalQuantity.ToString("N0", viCulture));
                            table.Cell().Element(FooterCell).Text(string.Empty);
                            table.Cell().Element(FooterCell).AlignRight()
                                .Text(totalAmount.ToString("#,0.##", viCulture));
                        }
                    });

                    col.Item().Height(8);

                    col.Item().Text(text =>
                    {
                        text.Span("- Tổng số tiền (viết bằng chữ): ").FontSize(9);
                        text.Span("...............................................................")
                            .FontSize(9);
                    });

                    col.Item().Text(text =>
                    {
                        text.Span("- Số chứng từ gốc kèm theo: ").FontSize(9);
                        text.Span(".......................................................")
                            .FontSize(9);
                    });
                });
            }

            private void ComposeFooter(IContainer container, OutboundReceiptPrintViewModel model)
            {
                var date = model.OutboundDate ?? DateTime.Now;

                container.PaddingTop(10).Column(col =>
                {
                    col.Item().AlignRight().Text(text =>
                    {
                        text.Span($"Ngày {date:dd} tháng {date:MM} năm {date:yyyy}").FontSize(10);
                    });

                    col.Item().Height(5);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("Người lập phiếu");
                        row.RelativeItem().AlignCenter().Text("Người nhận hàng");
                        row.RelativeItem().AlignCenter().Text("Thủ kho");
                        row.RelativeItem().AlignCenter()
                            .Text("Kế toán trưởng\n(Hoặc bộ phận có nhu cầu nhập)");
                        row.RelativeItem().AlignCenter().Text("Giám đốc");
                    });

                    col.Item().Row(row =>
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            row.RelativeItem().AlignCenter().Text(text =>
                            {
                                text.Span("(Ký, họ tên)").FontSize(9).Italic();
                            });
                        }
                    });
                });
            }

            private static IContainer HeaderCell(IContainer container)
            {
                return container
                    .PaddingVertical(4)
                    .PaddingHorizontal(2)
                    .Background(Colors.Grey.Lighten3)
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Medium)
                    .DefaultTextStyle(x => x.SemiBold());
            }

            private static IContainer Cell(IContainer container)
            {
                return container
                    .PaddingVertical(2)
                    .PaddingHorizontal(2)
                    .BorderBottom(0.5f)
                    .BorderColor(Colors.Grey.Lighten2);
            }

            private static IContainer FooterCell(IContainer container)
            {
                return container
                    .PaddingVertical(4)
                    .PaddingHorizontal(2)
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Medium);
            }
        }
    }
}
