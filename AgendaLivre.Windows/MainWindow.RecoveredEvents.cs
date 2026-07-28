using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
		private AppointmentAssistantRow? _appointmentAssistantCurrent;

		private readonly Dictionary<string, DateTime> _appointmentAssistantSnoozedUntilByAppointment = new Dictionary<string, DateTime>(StringComparer.Ordinal);

		private DatePickerTextBox? _appointmentDateTextBox;

		private bool _formattingAppointmentDate;

		private bool _appointmentTimelineDragging;

		private Point _appointmentTimelineDragStart;

		private double _appointmentTimelineDragStartOffset;

		private IReadOnlyList<decimal> _financeForecastConfirmedResults = Array.Empty<decimal>();

		private IReadOnlyList<decimal> _financeForecastCumulativeExpenses = Array.Empty<decimal>();

		private ToolTip? _financeForecastHoverToolTip;

		private IReadOnlyList<decimal> _financeForecastPotentialResults = Array.Empty<decimal>();

		private DateTime _financeForecastStartDate;

		private string _financeFunnelDefaultConversionText = "0%";

		private readonly Dictionary<string, FinanceHoverDetail> _financeFunnelHoverDetails = new Dictionary<string, FinanceHoverDetail>(StringComparer.Ordinal);

		private readonly Dictionary<string, FinanceHoverDetail> _financePyramidHoverDetails = new Dictionary<string, FinanceHoverDetail>(StringComparer.Ordinal);

		private bool _formattingInitialPhone;

		private int _homePeriodModeIndex;

		private string _lastAppointmentAssistantAnnouncementKey = "";

		private bool _marketingPhotoThemeSyncing;

		private bool _marketingPhotoSearchRunning;

		private const int MarketingPhotoSuggestionLimit = 24;

		private DateTime _marketingPhotosLoadedForDate = DateTime.MinValue;

		private readonly ObservableCollection<MarketingPhotoSuggestion> _marketingPhotoSuggestions = new ObservableCollection<MarketingPhotoSuggestion>();

		private DateTime _marketingSlotsDate = DateTime.Today;

		private string _marketingStudioChannel = "story";

		private MarketingPhotoSuggestion? _selectedMarketingPhoto;

		private IReadOnlyList<decimal> _reportsOccupancyDailyPercents = Array.Empty<decimal>();

		private IReadOnlyList<DateTime> _reportsOccupancyDailyDates = Array.Empty<DateTime>();

		private int _reportsPerformanceTotal;

		private int _reportsPerformanceFinalized;

		private int _reportsFunnelScheduled;

		private int _reportsFunnelConfirmed;

		private int _reportsFunnelAttended;

		private int _reportsFunnelReceived;

		private decimal _reportsFunnelScheduledGapValue;

		private decimal _reportsFunnelConfirmedGapValue;

		private decimal _reportsFunnelAttendedGapValue;

		private static readonly bool AppointmentAssistantEnabled;

		private async Task ApplyMarketingPhotoAsync(MarketingPhotoSuggestion suggestion)
		{
			MarketingPhotoStatusText.Text = "Aplicando a foto de " + suggestion.CreatorDisplay + "...";
			try
			{
				MarketingPhotoSuggestion resolvedSuggestion = await ResolveMarketingPhotoAsync(suggestion);
				BitmapSource bitmap = string.IsNullOrWhiteSpace(resolvedSuggestion.ImageUrl) &&
					string.IsNullOrWhiteSpace(resolvedSuggestion.ThumbnailUrl)
						? resolvedSuggestion.Thumbnail ?? throw new InvalidOperationException("A foto editorial local não pôde ser carregada.")
						: await DownloadMarketingPhotoAsync(resolvedSuggestion.ImageUrl, resolvedSuggestion.ThumbnailUrl);
				MarketingStudioPreviewCard.Background = new ImageBrush(bitmap)
				{
					Stretch = Stretch.UniformToFill,
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center
				};
				MarketingStudioSelectedImageCard.Background = new ImageBrush(bitmap)
				{
					Stretch = Stretch.UniformToFill,
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center
				};
				if (_marketingEditorInitialized)
				{
					PushMarketingEditorUndo();
					_marketingEditorApplying = true;
					try
					{
						MarketingEditorPhotoZoomSlider.Value = 1;
						MarketingEditorPhotoXSlider.Value = 0;
						MarketingEditorPhotoYSlider.Value = 0;
					}
					finally
					{
						_marketingEditorApplying = false;
					}
					SetMarketingEditorPhoto(bitmap);
					MarketingEditorPhotoVisibleCheck.IsChecked = true;
					MarketingEditorAttributionText.Text = resolvedSuggestion.CreditLine;
					MarketingEditorAttributionText.Visibility = Visibility.Visible;
					SelectMarketingEditorLayer("photo");
				}
				_selectedMarketingPhoto = resolvedSuggestion;
				MarketingPhotoAttributionText.Text = resolvedSuggestion.CreditLine;
				MarketingStudioPreviewAttributionText.Text = resolvedSuggestion.CreditLine;
				MarketingPhotoCreditPanel.Visibility = Visibility.Visible;
				MarketingStudioPreviewAttributionText.Visibility = Visibility.Visible;
				MarketingPhotoStatusText.Text = "Foto aplicada. Agora ajuste o texto ou publique no WhatsApp.";
				ShowStatus("Foto aplicada à publicação com os créditos incluídos na arte.");
			}
			catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is InvalidOperationException || ex is IOException || ex is NotSupportedException || ex is FormatException) ? 1 : 0) != 0)
			{
				MarketingPhotoStatusText.Text = "Não consegui carregar essa foto. Escolha outra sugestão.";
			}
		}

		private sealed record AppointmentAssistantRow(Appointment Appointment, AppointmentAssistantState State, DateTime PaymentPromptAt, bool UsesLearnedTiming, int Priority);

		private enum AppointmentAssistantState
		{
			Upcoming,
			InProgress,
			AttendanceCheck,
			Payment
		}

		private void AppointmentDateTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Invalid comparison between Unknown and I4
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Invalid comparison between Unknown and I4
			if (!_formattingAppointmentDate && sender is DatePickerTextBox { SelectionLength: <=0 } textBox)
			{
				string text = textBox.Text ?? "";
				if ((int)e.Key == 2 && textBox.CaretIndex > 0 && textBox.CaretIndex <= text.Length && text[textBox.CaretIndex - 1] == '/')
				{
					textBox.CaretIndex--;
				}
				else if ((int)e.Key == 32 && textBox.CaretIndex >= 0 && textBox.CaretIndex < text.Length && text[textBox.CaretIndex] == '/')
				{
					textBox.CaretIndex++;
				}
			}
		}

		private void AppointmentDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!_formattingAppointmentDate && sender is DatePickerTextBox textBox)
			{
				FormatAppointmentDateText(textBox);
			}
		}

		private void ConfirmAppointmentAttendance(Appointment appointment)
		{
			Appointment currentAppointment = _data.Appointments.FirstOrDefault((Appointment item) => item.Id == appointment.Id);
			if (currentAppointment == null)
			{
				RefreshAll();
				ShowStatus("O agendamento não está mais disponível.");
				return;
			}
			AppointmentStatus status = currentAppointment.Status;
			if (status != AppointmentStatus.Scheduled && status != AppointmentStatus.Confirmed)
			{
				RefreshAll();
				ShowStatus("O status desse atendimento já foi atualizado.");
				return;
			}
			currentAppointment.Status = AppointmentStatus.Waiting;
			currentAppointment.UpdatedAt = DateTime.Now;
			_store.Save(_data);
			RefreshAll();
			ShowStatus("Presença confirmada. Agora você já pode confirmar o pagamento.");
		}

		private AppointmentAssistantRow? CreateAppointmentAssistantRow(Appointment appointment, DateTime now)
		{
			if (now < appointment.Start.AddMinutes(-5.0))
			{
				return null;
			}
			bool learnedTiming;
			DateTime paymentPromptAt = AppointmentAssistantPaymentPromptAt(appointment, out learnedTiming);
			if (now >= paymentPromptAt)
			{
				AppointmentStatus status = appointment.Status;
				if ((uint)status <= 1u)
				{
					return new AppointmentAssistantRow(appointment, AppointmentAssistantState.AttendanceCheck, paymentPromptAt, learnedTiming, 2);
				}
				return new AppointmentAssistantRow(appointment, AppointmentAssistantState.Payment, paymentPromptAt, learnedTiming, 1);
			}
			if (now >= appointment.Start)
			{
				return new AppointmentAssistantRow(appointment, AppointmentAssistantState.InProgress, paymentPromptAt, learnedTiming, 3);
			}
			return new AppointmentAssistantRow(appointment, AppointmentAssistantState.Upcoming, paymentPromptAt, learnedTiming, 0);
		}

		private static Border CreateFinanceHoverToolTipContent(FinanceHoverDetail detail)
		{
			StackPanel body = new StackPanel();
			Grid header = new Grid
			{
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
			};
			header.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			header.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			header.Children.Add(new TextBlock
			{
				Text = detail.Title,
				Foreground = InkBrush,
				FontSize = 12.0,
				FontWeight = FontWeights.Bold,
				VerticalAlignment = VerticalAlignment.Center
			});
			TextBlock valueText = new TextBlock
			{
				Text = detail.Value,
				Foreground = AccentTextBrush,
				FontSize = 13.0,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(12.0, 0.0, 0.0, 0.0)
			};
			Grid.SetColumn(valueText, 1);
			header.Children.Add(valueText);
			body.Children.Add(header);
			body.Children.Add(new TextBlock
			{
				Text = detail.Description,
				Foreground = MutedBrush,
				FontSize = 10.5,
				TextWrapping = TextWrapping.Wrap,
				LineHeight = 15.0
			});
			if (!string.IsNullOrWhiteSpace(detail.Metric))
			{
				body.Children.Add(new Border
				{
					Background = AccentSoftBrush,
					CornerRadius = new CornerRadius(8.0),
					Padding = new Thickness(8.0, 4.0, 8.0, 4.0),
					Margin = new Thickness(0.0, 9.0, 0.0, 0.0),
					HorizontalAlignment = HorizontalAlignment.Left,
					Child = new TextBlock
					{
						Text = detail.Metric,
						Foreground = AccentTextBrush,
						FontSize = 9.5,
						FontWeight = FontWeights.SemiBold
					}
				});
			}
			body.Children.Add(new Border
			{
				Height = 1.0,
				Background = Solid("#EEE9E5"),
				Margin = new Thickness(0.0, 10.0, 0.0, 8.0)
			});
			body.Children.Add(new TextBlock
			{
				Text = "Como é calculado: " + detail.Formula,
				Foreground = Solid("#49443F"),
				FontSize = 9.5,
				TextWrapping = TextWrapping.Wrap
			});
			body.Children.Add(new TextBlock
			{
				Text = "Fonte: " + detail.Source,
				Foreground = Solid("#8D8782"),
				FontSize = 9.0,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 5.0, 0.0, 0.0)
			});
			return new Border
			{
				Width = 300.0,
				Background = Brushes.White,
				BorderBrush = Solid("#D8D3CE"),
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(12.0),
				Padding = new Thickness(13.0),
				Effect = new DropShadowEffect
				{
					Color = Color.FromRgb(28, 27, 26),
					BlurRadius = 22.0,
					ShadowDepth = 4.0,
					Opacity = 0.18
				},
				Child = body
			};
		}

		private void DrawReportsOption3AppointmentsChart()
		{
			//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0580: Unknown result type (might be due to invalid IL or missing references)
			//IL_0585: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0328: Unknown result type (might be due to invalid IL or missing references)
			//IL_032d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0339: Unknown result type (might be due to invalid IL or missing references)
			//IL_033e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0381: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0409: Unknown result type (might be due to invalid IL or missing references)
			//IL_0487: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_04de: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0503: Unknown result type (might be due to invalid IL or missing references)
			ReportsOption3AppointmentsCanvas.Children.Clear();
			if (ReportsOption3AppointmentsCanvas.Visibility != Visibility.Visible)
			{
				return;
			}
			double width = ReportsOption3AppointmentsCanvas.ActualWidth;
			double height = ReportsOption3AppointmentsCanvas.ActualHeight;
			List<ReportChartRow> rows = _reportsColumnChartRows.ToList();
			if (width < 120.0 || height < 120.0 || rows.Count == 0)
			{
				return;
			}
			double chartWidth = Math.Max(1.0, width - 38.0 - 12.0);
			double chartHeight = Math.Max(1.0, height - 13.0 - 48.0);
			decimal highest = Math.Max(0m, rows.Max((ReportChartRow reportChartRow) => reportChartRow.Value));
			double axisMax = Math.Max(4.0, Math.Ceiling((double)highest / 2.0) * 2.0);
			for (int index = 0; index <= 4; index++)
			{
				double y = 13.0 + chartHeight * (double)index / 4.0;
				ReportsOption3AppointmentsCanvas.Children.Add(new Line
				{
					X1 = 38.0,
					X2 = 38.0 + chartWidth,
					Y1 = y,
					Y2 = y,
					Stroke = LineBrush,
					StrokeThickness = 1.0,
					StrokeDashArray = ((index == 4) ? null : new DoubleCollection { 3.0, 3.0 })
				});
				TextBlock axisLabel = new TextBlock
				{
					Text = (axisMax * (double)(4 - index) / 4.0).ToString("N0", Brazil),
					Foreground = MutedBrush,
					FontSize = 10.5,
					Width = 28.0,
					TextAlignment = TextAlignment.Left
				};
				Canvas.SetLeft(axisLabel, 0.0);
				Canvas.SetTop(axisLabel, Math.Max(0.0, y - 8.0));
				ReportsOption3AppointmentsCanvas.Children.Add(axisLabel);
			}
			List<Point> points = ((IEnumerable<ReportChartRow>)rows).Select((Func<ReportChartRow, int, Point>)((ReportChartRow reportChartRow, int num) => new Point(38.0 + chartWidth * ((double)num + 0.5) / (double)rows.Count, 13.0 + chartHeight - (double)(reportChartRow.Value / (decimal)axisMax) * chartHeight))).ToList();
			if (points.Count > 1)
			{
				PathFigure areaFigure = new PathFigure
				{
					StartPoint = points[0],
					IsClosed = true
				};
				for (int index2 = 1; index2 < points.Count - 1; index2++)
				{
					areaFigure.Segments.Add(new LineSegment(points[index2], isStroked: true));
				}
				Point beforeLast = points[points.Count - 2];
				Point last = points[points.Count - 1];
				double controlDistance = Math.Max(24.0, (last.X - beforeLast.X) * 0.58);
				areaFigure.Segments.Add(new BezierSegment(new Point(beforeLast.X + controlDistance, beforeLast.Y), new Point(last.X - controlDistance * 0.48, last.Y), last, isStroked: true));
				areaFigure.Segments.Add(new LineSegment(new Point(last.X, 13.0 + chartHeight), isStroked: true));
				PathSegmentCollection segments = areaFigure.Segments;
				Point val = points[0];
				segments.Add(new LineSegment(new Point(val.X, 13.0 + chartHeight), isStroked: true));
				ReportsOption3AppointmentsCanvas.Children.Add(new System.Windows.Shapes.Path
				{
					Data = new PathGeometry(new PathFigure[1] { areaFigure }),
					Fill = AccentSoftBrush,
					Opacity = ((highest <= 0m) ? 0.0 : 0.72)
				});
				PathFigure lineFigure = new PathFigure
				{
					StartPoint = points[0]
				};
				for (int index3 = 1; index3 < points.Count - 1; index3++)
				{
					lineFigure.Segments.Add(new LineSegment(points[index3], isStroked: true));
				}
				lineFigure.Segments.Add(new BezierSegment(new Point(beforeLast.X + controlDistance, beforeLast.Y), new Point(last.X - controlDistance * 0.48, last.Y), last, isStroked: true));
				ReportsOption3AppointmentsCanvas.Children.Add(new System.Windows.Shapes.Path
				{
					Data = new PathGeometry(new PathFigure[1] { lineFigure }),
					Stroke = AccentBrush,
					StrokeThickness = 2.2,
					StrokeLineJoin = PenLineJoin.Round
				});
			}
			for (int index4 = 0; index4 < rows.Count; index4++)
			{
				ReportChartRow row = rows[index4];
				Point point = points[index4];
				if (row.Value > 0m)
				{
					double barHeight = Math.Max(4.0, 13.0 + chartHeight - point.Y);
					Border bar = new Border
					{
						Width = 22.0,
						Height = barHeight,
						Background = AccentBrush,
						CornerRadius = new CornerRadius(4.0, 4.0, 0.0, 0.0)
					};
					Canvas.SetLeft(bar, point.X - 11.0);
					Canvas.SetTop(bar, point.Y);
					ReportsOption3AppointmentsCanvas.Children.Add(bar);
				}
				else
				{
					Ellipse dot = new Ellipse
					{
						Width = 7.0,
						Height = 7.0,
						Fill = AccentBrush
					};
					Canvas.SetLeft(dot, point.X - 3.5);
					Canvas.SetTop(dot, 13.0 + chartHeight - 3.5);
					ReportsOption3AppointmentsCanvas.Children.Add(dot);
				}
				TextBlock value = new TextBlock
				{
					Text = row.ValueText,
					Foreground = AccentBrush,
					FontSize = 12.0,
					FontWeight = FontWeights.Bold,
					Width = 58.0,
					TextAlignment = TextAlignment.Center
				};
				Canvas.SetLeft(value, point.X - 29.0);
				Canvas.SetTop(value, Math.Max(0.0, ((row.Value > 0m) ? point.Y : (13.0 + chartHeight)) - 23.0));
				ReportsOption3AppointmentsCanvas.Children.Add(value);
				string[] labelParts = row.Label.Split(',', 2, StringSplitOptions.TrimEntries);
				TextBlock dayLabel = new TextBlock
				{
					Text = labelParts[0],
					Foreground = InkBrush,
					FontSize = 10.5,
					Width = 72.0,
					TextAlignment = TextAlignment.Center
				};
				Canvas.SetLeft(dayLabel, point.X - 36.0);
				Canvas.SetTop(dayLabel, 13.0 + chartHeight + 7.0);
				ReportsOption3AppointmentsCanvas.Children.Add(dayLabel);
				TextBlock dateLabel = new TextBlock
				{
					Text = ((labelParts.Length > 1) ? labelParts[1] : ""),
					Foreground = MutedBrush,
					FontSize = 9.5,
					Width = 72.0,
					TextAlignment = TextAlignment.Center
				};
				Canvas.SetLeft(dateLabel, point.X - 36.0);
				Canvas.SetTop(dateLabel, 13.0 + chartHeight + 24.0);
				ReportsOption3AppointmentsCanvas.Children.Add(dateLabel);
			}
		}

		private void DrawReportsOption3OccupancyChart()
		{
			//IL_014f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0175: Unknown result type (might be due to invalid IL or missing references)
			//IL_018e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_0197: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
			Canvas canvas = ReportsOption3OccupancyChartCanvas;
			canvas.Children.Clear();
			if (_reportsOccupancyDailyPercents.Count != 7 || _reportsOccupancyDailyDates.Count != 7)
			{
				return;
			}
			double width = canvas.ActualWidth;
			double height = canvas.ActualHeight;
			if (width < 120.0 || height < 70.0)
			{
				return;
			}
			double plotWidth = width - 18.0 - 14.0;
			double plotHeight = height - 18.0 - 31.0;
			double baseline = 18.0 + plotHeight;
			canvas.Children.Add(new Line
			{
				X1 = 18.0,
				X2 = width - 14.0,
				Y1 = baseline,
				Y2 = baseline,
				Stroke = LineBrush,
				StrokeThickness = 1.0
			});
			PointCollection points = new PointCollection();
			for (int index = 0; index < 7; index++)
			{
				double x = 18.0 + plotWidth * (double)index / 6.0;
				double y = 18.0 + plotHeight * (1.0 - (double)_reportsOccupancyDailyPercents[index] / 100.0);
				points.Add(new Point(x, y));
			}
			PointCollection areaPoints = new PointCollection
			{
				new Point(18.0, baseline)
			};
			foreach (Point point in points)
			{
				areaPoints.Add(point);
			}
			areaPoints.Add(new Point(width - 14.0, baseline));
			canvas.Children.Add(new Polygon
			{
				Points = areaPoints,
				Fill = new SolidColorBrush(Color.FromArgb(34, 240, 100, 35)),
				IsHitTestVisible = false
			});
			canvas.Children.Add(new Polyline
			{
				Points = points,
				Stroke = AccentBrush,
				StrokeThickness = 2.2,
				StrokeLineJoin = PenLineJoin.Round,
				IsHitTestVisible = false
			});
			int peakIndex = (from item in _reportsOccupancyDailyPercents.Select((decimal item, int item2) => (value: item, index: item2))
				orderby item.value descending
				select item).First().index;
			for (int index2 = 0; index2 < 7; index2++)
			{
				Point point2 = points[index2];
				decimal value = _reportsOccupancyDailyPercents[index2];
				DateTime date = _reportsOccupancyDailyDates[index2];
				if (index2 == peakIndex && value > 0m)
				{
					Ellipse ring = new Ellipse
					{
						Width = 14.0,
						Height = 14.0,
						Stroke = AccentBrush,
						StrokeThickness = 1.5,
						Fill = AccentSoftBrush
					};
					Canvas.SetLeft(ring, point2.X - 7.0);
					Canvas.SetTop(ring, point2.Y - 7.0);
					canvas.Children.Add(ring);
				}
				Ellipse dot = new Ellipse
				{
					Width = 8.0,
					Height = 8.0,
					Fill = AccentBrush,
					Stroke = Brushes.White,
					StrokeThickness = 1.0,
					Cursor = Cursors.Hand,
					ToolTip = $"{date:ddd, dd/MM}\nOcupação: {value:N0}%\nBase: minutos agendados ÷ capacidade configurada"
				};
				Canvas.SetLeft(dot, point2.X - 4.0);
				Canvas.SetTop(dot, point2.Y - 4.0);
				canvas.Children.Add(dot);
				TextBlock valueText = new TextBlock
				{
					Text = $"{value:N0}%",
					Foreground = InkBrush,
					FontSize = 8.5,
					FontWeight = FontWeights.Bold,
					Width = 38.0,
					TextAlignment = TextAlignment.Center
				};
				Canvas.SetLeft(valueText, point2.X - 19.0);
				Canvas.SetTop(valueText, Math.Max(0.0, point2.Y - 18.0));
				canvas.Children.Add(valueText);
				string day = Brazil.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek).TrimEnd('.').ToLower(Brazil);
				TextBlock dateText = new TextBlock
				{
					Text = $"{day}\n{date:dd/MM}",
					Foreground = ((index2 == 6) ? AccentBrush : MutedBrush),
					FontSize = 8.0,
					FontWeight = ((index2 == 6) ? FontWeights.Bold : FontWeights.Normal),
					Width = 44.0,
					TextAlignment = TextAlignment.Center,
					LineHeight = 9.0
				};
				Canvas.SetLeft(dateText, point2.X - 22.0);
				Canvas.SetTop(dateText, baseline + 4.0);
				canvas.Children.Add(dateText);
			}
		}

		private void DrawReportsPerformanceDonut()
		{
			Canvas canvas = ReportsPerformanceDonutCanvas;
			canvas.Children.Clear();
			double width = ((canvas.ActualWidth > 0.0) ? canvas.ActualWidth : canvas.Width);
			double height = ((canvas.ActualHeight > 0.0) ? canvas.ActualHeight : canvas.Height);
			if (!(width <= 0.0) && !(height <= 0.0))
			{
				double centerX = width / 2.0;
				double centerY = height / 2.0;
				double outer = Math.Min(width, height) / 2.0 - 8.0;
				double inner = outer - 10.0;
				Ellipse ring = new Ellipse
				{
					Width = outer * 2.0,
					Height = outer * 2.0,
					Stroke = Solid("#4C4946"),
					StrokeThickness = outer - inner,
					Fill = Brushes.Transparent
				};
				Canvas.SetLeft(ring, centerX - outer);
				Canvas.SetTop(ring, centerY - outer);
				canvas.Children.Add(ring);
				if (_reportsPerformanceTotal > 0 && _reportsPerformanceFinalized > 0)
				{
					double sweep = Math.Max(1.0, Math.Min(359.8, (double)_reportsPerformanceFinalized * 360.0 / (double)_reportsPerformanceTotal));
					canvas.Children.Add(CreateDonutSlice(centerX, centerY, outer, inner, -90.0, sweep, AccentBrush));
				}
				if (_reportsPerformanceTotal > 0)
				{
					Ellipse dot = new Ellipse
					{
						Width = 10.0,
						Height = 10.0,
						Fill = AccentBrush
					};
					Canvas.SetLeft(dot, centerX - 5.0);
					Canvas.SetTop(dot, centerY - outer - 5.0);
					canvas.Children.Add(dot);
				}
				decimal conversion = ReportPercent(_reportsPerformanceFinalized, _reportsPerformanceTotal);
				AddCenteredReportDonutText(canvas, $"{conversion:N0}%", "conversão", centerX, centerY, Brushes.White, Solid("#B9B2AC"));
			}
		}

		private static void AddCenteredReportDonutText(Canvas canvas, string value, string label, double centerX, double centerY, Brush valueBrush, Brush labelBrush)
		{
			TextBlock valueText = new TextBlock
			{
				Text = value,
				Foreground = valueBrush,
				FontSize = 22.0,
				FontWeight = FontWeights.Bold,
				Width = 72.0,
				TextAlignment = TextAlignment.Center
			};
			Canvas.SetLeft(valueText, centerX - 36.0);
			Canvas.SetTop(valueText, centerY - 25.0);
			canvas.Children.Add(valueText);
			TextBlock labelText = new TextBlock
			{
				Text = label,
				Foreground = labelBrush,
				FontSize = 10.0,
				Width = 72.0,
				TextAlignment = TextAlignment.Center
			};
			Canvas.SetLeft(labelText, centerX - 36.0);
			Canvas.SetTop(labelText, centerY + 5.0);
			canvas.Children.Add(labelText);
		}

		private IEnumerable<Polygon> FinanceFunnelShapes()
		{
			yield return FinanceFunnelScheduledShape;
			yield return FinanceFunnelConfirmedShape;
			yield return FinanceFunnelCompletedShape;
			yield return FinanceFunnelReceivedShape;
			yield return FinanceFunnelAvailableShape;
		}

		private sealed record FinanceHoverDetail(string Title, string Value, string Description, string Formula, string Source, string Metric, decimal RawValue);

		private IEnumerable<Polygon> FinancePyramidShapes()
		{
			yield return FinancePyramidAppointmentsShape;
			yield return FinancePyramidReceivablesShape;
			yield return FinancePyramidProductsShape;
			yield return FinancePyramidManualShape;
		}

		private void FormatAppointmentDateText(DatePickerTextBox textBox)
		{
			string original = textBox.Text ?? "";
			int digitCaret = OnlyDigits(original[..Math.Min(textBox.CaretIndex, original.Length)]).Length;
			string digits = OnlyDigits(original);
			if (digits.Length > 8)
			{
				digits = digits.Substring(0, 8);
			}
			digitCaret = Math.Min(digitCaret, digits.Length);
			string formatted = FormatAppointmentDateInput(digits);
			DateTime parsedDate = default(DateTime);
			bool hasValidDate = digits.Length == 8 && DateTime.TryParseExact(digits, "ddMMyyyy", Brazil, DateTimeStyles.None, out parsedDate);
			_formattingAppointmentDate = true;
			try
			{
				AppointmentDatePicker.SelectedDate = (hasValidDate ? new DateTime?(parsedDate.Date) : ((DateTime?)null));
				textBox.Text = formatted;
				textBox.CaretIndex = AppointmentDateCaretIndex(formatted, digitCaret);
			}
			finally
			{
				_formattingAppointmentDate = false;
			}
			RefreshAppointmentEditorSummary();
		}

		private static string FormatAppointmentDateInput(string digits)
		{
			if (digits.Length > 8)
			{
				digits = digits.Substring(0, 8);
			}
			switch (digits.Length)
			{
			case 0:
				return "";
			case 1:
				return digits;
			case 2:
				return digits + "/";
			case 3:
			{
				string text3 = digits.Substring(0, 2);
				string text = digits;
				return text3 + "/" + text.Substring(2, text.Length - 2);
			}
			case 4:
			{
				string text2 = digits.Substring(0, 2);
				string text = digits;
				return text2 + "/" + text.Substring(2, text.Length - 2) + "/";
			}
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
				defaultInterpolatedStringHandler.AppendFormatted(digits.Substring(0, 2));
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(digits.Substring(2, 2));
				defaultInterpolatedStringHandler.AppendLiteral("/");
				string text = digits;
				defaultInterpolatedStringHandler.AppendFormatted(text.Substring(4, text.Length - 4));
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			}
		}

		private static int AppointmentDateCaretIndex(string formatted, int digitCount)
		{
			int caret;
			for (caret = CaretIndexAfterDigits(formatted, digitCount); caret < formatted.Length && formatted[caret] == '/'; caret++)
			{
			}
			return caret;
		}

		private bool HasMarketingStudioDefaultText(TextBox textBox)
		{
			if (textBox != MarketingStudioTitleTextBox || !string.Equals(textBox.Text, "Horários livres", StringComparison.Ordinal))
			{
				if (textBox == MarketingStudioCopyTextBox)
				{
					return string.Equals(textBox.Text, "Temos horários disponíveis para você. Escolha o melhor momento e faça sua reserva.", StringComparison.Ordinal);
				}
				return false;
			}
			return true;
		}

		public sealed record MarketingPhotoSuggestion(string Title, string Creator, string License, string LicenseUrl, string ImageUrl, string ThumbnailUrl, string LandingUrl, BitmapSource? Thumbnail = null, string OpenverseId = "", string Category = "")
		{
			public string CreatorDisplay
			{
				get
				{
					if (!string.IsNullOrWhiteSpace(Creator))
					{
						return Creator;
					}
					return "Autor não informado";
				}
			}
	
			public string LicenseDisplay
			{
				get
				{
					string value = License.Trim().ToLowerInvariant();
					switch (value)
					{
					case "cc0":
						return "CC0";
					case "pdm":
						return "Domínio público";
					case "by":
						return "CC BY";
					case "by-sa":
						return "CC BY-SA";
					default:
						if (value.Length > 0)
						{
							return "CC " + value.ToUpperInvariant();
						}
						return "Licença aberta";
					}
				}
			}
	
			public string CreditLine => "Foto: " + CreatorDisplay + " • " + LicenseDisplay;
		}

		private static string MarketingPhotoThemeLabel(string? category, string fallback)
		{
			return category switch
			{
				"hair" => "cabelo com acabamento sofisticado", 
				"nails" => "mãos, unhas e detalhes delicados", 
				"makeup" => "maquiagem e beleza editorial", 
				"aesthetics" => "pele, tratamentos faciais e estética", 
				"spa" => "spa, massagens e autocuidado", 
				"beauty" => "beleza premium", 
				_ => fallback, 
			};
		}

		private void MarketingStudioBusinessIdentity()
		{
			MarketingStudioPreviewBusinessText.Text = BusinessDisplayName();
			MarketingStudioPreviewPhoneText.Text = FormatPhone(string.IsNullOrWhiteSpace(_data.Settings.BusinessPhone) ? _data.Settings.AccountPhone : _data.Settings.BusinessPhone);
			if (_marketingEditorInitialized)
			{
				MarketingEditorBusinessText.Text = MarketingStudioPreviewBusinessText.Text;
				MarketingEditorPhoneText.Text = MarketingStudioPreviewPhoneText.Text;
			}
		}

		private string MarketingStudioCopyValue()
		{
			if (!string.IsNullOrWhiteSpace(MarketingStudioCopyTextBox?.Text))
			{
				return MarketingStudioCopyTextBox.Text.Trim();
			}
			return "Temos horários disponíveis para você. Escolha o melhor momento e faça sua reserva.";
		}

		private string MarketingStudioTitleValue()
		{
			if (!string.IsNullOrWhiteSpace(MarketingStudioTitleTextBox?.Text))
			{
				return MarketingStudioTitleTextBox.Text.Trim();
			}
			return "Horários livres";
		}

		private void OpenAppointmentFromAssistant(Appointment appointment)
		{
			_selectedDate = appointment.Start.Date;
			UpdateDateFilterButton();
			ShowMainPage(MainPage.Agenda);
			RefreshAll(appointment.Id);
			_selectedAppointment = appointment;
			LoadEditor(appointment);
			OpenAppointmentEditorModal();
			ShowStatus(appointment.CustomerName + " aberto pelo assistente da agenda.");
		}

		private void RefreshAppointmentAssistant()
		{
			if (AppointmentAssistantFloatingPanel == null)
			{
				return;
			}
			if (!AppointmentAssistantEnabled)
			{
				_appointmentAssistantCurrent = null;
				_lastAppointmentAssistantAnnouncementKey = "";
				AppointmentAssistantFloatingPanel.Visibility = Visibility.Collapsed;
				return;
			}
			DateTime now = DateTime.Now;
			foreach (string expiredId in (from item in _appointmentAssistantSnoozedUntilByAppointment
				where item.Value <= now
				select item.Key).ToList())
			{
				_appointmentAssistantSnoozedUntilByAppointment.Remove(expiredId);
			}
			DateTime value;
			List<AppointmentAssistantRow> rows = (from AppointmentAssistantRow item in from item in _data.Appointments.Where(delegate(Appointment item)
					{
						bool flag = item.Start >= now.AddHours(-12.0) && item.Start <= now.AddMinutes(5.0) && !IsPreviewAppointment(item);
						if (flag)
						{
							AppointmentStatus status = item.Status;
							bool flag2 = (uint)status <= 3u;
							flag = flag2;
						}
						return flag;
					})
					select CreateAppointmentAssistantRow(item, now) into item
					where (object)item != null
					select item
				where !_appointmentAssistantSnoozedUntilByAppointment.TryGetValue(item.Appointment.Id, out value) || value <= now
				orderby item.Priority, (item.State != AppointmentAssistantState.Payment) ? item.Appointment.Start : item.PaymentPromptAt
				select item).ToList();
			_appointmentAssistantCurrent = rows.FirstOrDefault();
			if ((object)_appointmentAssistantCurrent == null)
			{
				_lastAppointmentAssistantAnnouncementKey = "";
				RefreshWhatsAppLauncherVisibility();
				return;
			}
			AppointmentAssistantRow row = _appointmentAssistantCurrent;
			Appointment appointment = row.Appointment;
			int extraCount = rows.Count - 1;
			TextBlock appointmentAssistantHeaderText = AppointmentAssistantHeaderText;
			appointmentAssistantHeaderText.Text = row.State switch
			{
				AppointmentAssistantState.Payment => "Fechar o caixa do atendimento", 
				AppointmentAssistantState.AttendanceCheck => "Verificar atendimento", 
				_ => "Agora na agenda", 
			};
			AppointmentAssistantSummaryText.Text = ((rows.Count == 1) ? "1 atendimento precisa de atenção" : $"{rows.Count} atendimentos precisam de atenção");
			AppointmentAssistantMoreBadge.Visibility = ((extraCount <= 0) ? Visibility.Collapsed : Visibility.Visible);
			AppointmentAssistantMoreText.Text = $"+{extraCount}";
			AppointmentAssistantAppointmentText.Text = FirstFilled(appointment.CustomerName, "Cliente") + " • " + FirstFilled(appointment.ServiceName, "Atendimento");
			AppointmentAssistantDetailText.Text = $"{appointment.Start:HH:mm}–{appointment.End:HH:mm} • {FirstFilled(appointment.ProfessionalName, "Equipe")}";
			AppointmentAssistantPriceText.Text = ((appointment.Price > 0m) ? appointment.Price.ToString("C", Brazil) : "Sem cobrança");
			switch (row.State)
			{
			case AppointmentAssistantState.Upcoming:
			{
				int minutes = Math.Max(1, (int)Math.Ceiling((appointment.Start - now).TotalMinutes));
				AppointmentAssistantHeaderIcon.Kind = PackIconKind.CalendarClock;
				AppointmentAssistantStateIcon.Kind = PackIconKind.ClockOutline;
				AppointmentAssistantStateText.Text = ((minutes <= 1) ? "Começa agora" : $"Começa em {minutes} min");
				AppointmentAssistantLearningText.Text = "Aviso exibido 5 minutos antes para você se preparar.";
				AppointmentAssistantActionButton.Content = "Ver agendamento";
				AutomationProperties.SetName((DependencyObject)(object)AppointmentAssistantActionButton, "Ver agendamento que começará em breve");
				break;
			}
			case AppointmentAssistantState.InProgress:
				AppointmentAssistantHeaderIcon.Kind = PackIconKind.CalendarClock;
				AppointmentAssistantStateIcon.Kind = PackIconKind.ProgressClock;
				AppointmentAssistantStateText.Text = "Atendimento em andamento";
				AppointmentAssistantLearningText.Text = (row.UsesLearnedTiming ? $"Pagamento previsto para {row.PaymentPromptAt:HH:mm}, ajustado pelo histórico deste serviço." : $"Pagamento previsto para {row.PaymentPromptAt:HH:mm}, conforme a duração cadastrada.");
				AppointmentAssistantActionButton.Content = "Ver atendimento";
				AutomationProperties.SetName((DependencyObject)(object)AppointmentAssistantActionButton, "Ver atendimento em andamento");
				break;
			case AppointmentAssistantState.AttendanceCheck:
				AppointmentAssistantHeaderIcon.Kind = PackIconKind.CalendarCheckOutline;
				AppointmentAssistantStateIcon.Kind = PackIconKind.AccountCheckOutline;
				AppointmentAssistantStateText.Text = "O atendimento aconteceu?";
				AppointmentAssistantLearningText.Text = "Confirme a presença antes de registrar o pagamento. Se o cliente faltou, abra o agendamento.";
				AppointmentAssistantActionButton.Content = "Sim, aconteceu";
				AutomationProperties.SetName((DependencyObject)(object)AppointmentAssistantActionButton, "Confirmar que o atendimento aconteceu");
				break;
			default:
				AppointmentAssistantHeaderIcon.Kind = PackIconKind.CashMultiple;
				AppointmentAssistantStateIcon.Kind = PackIconKind.CashMultiple;
				AppointmentAssistantStateText.Text = ((appointment.Price > 0m) ? "Hora de confirmar o pagamento" : "Hora de finalizar o atendimento");
				AppointmentAssistantLearningText.Text = (row.UsesLearnedTiming ? "Horário ajustado pelo histórico local. Confirme somente se o atendimento terminou." : "Chegou ao fim previsto. Confirme somente se o atendimento terminou.");
				AppointmentAssistantActionButton.Content = ((appointment.Price > 0m) ? "Confirmar pagamento" : "Finalizar atendimento");
				AutomationProperties.SetName((DependencyObject)(object)AppointmentAssistantActionButton, (appointment.Price > 0m) ? "Confirmar pagamento do atendimento" : "Finalizar atendimento sem cobrança");
				break;
			}
			RefreshWhatsAppLauncherVisibility();
		}

		private async Task RefreshMarketingPhotosAsync(string? query = null, bool useDailyTheme = false)
		{
			if (_marketingPhotoSearchRunning || MarketingPhotoStatusText == null)
			{
				return;
			}
			(string, string)[] dailyThemes = new(string, string)[7]
			{
				("Ritual de spa", "spa"),
				("Beleza natural", "beauty"),
				("Cabelo em destaque", "hair"),
				("Pele e bem-estar", "spa"),
				("Maquiagem elegante", "makeup"),
				("Beleza premium", "beauty"),
				("Ritual de spa", "spa")
			};
			(string Label, string Category) dailyTheme = dailyThemes[(int)DateTime.Today.DayOfWeek];
			string text;
			if (string.IsNullOrWhiteSpace(query))
			{
				(text, _) = dailyTheme;
			}
			else
			{
				text = query.Trim();
			}
			string searchQuery = text;
			string curatedCategory = (useDailyTheme ? dailyTheme.Category : CuratedMarketingCategory(searchQuery));
			UpdateMarketingPhotoThemeSelection(curatedCategory);
			if (useDailyTheme)
			{
				MarketingPhotoSearchTextBox.Text = dailyTheme.Label;
			}
			else if (!string.IsNullOrWhiteSpace(query))
			{
				MarketingPhotoSearchTextBox.Text = searchQuery;
			}
			_marketingPhotoSearchRunning = true;
			MarketingPhotoSearchButton.IsEnabled = false;
			MarketingPhotoStatusText.Text = (useDailyTheme ? ("Abrindo a curadoria de hoje: " + dailyTheme.Label + "...") : ("Procurando fotos elegantes para “" + searchQuery + "”..."));
			try
			{
				List<MarketingPhotoSuggestion> suggestions =
					BuildCuratedMarketingSuggestions(curatedCategory ?? "beauty");
				_marketingPhotoSuggestions.Clear();
				foreach (MarketingPhotoSuggestion localSuggestion in suggestions
					.Where(item => item.Thumbnail != null)
					.Take(MarketingPhotoSuggestionLimit))
				{
					_marketingPhotoSuggestions.Add(localSuggestion);
				}
				IReadOnlyList<MarketingPhotoSuggestion> hydrated =
					await HydrateMarketingPhotoSuggestionsAsync(suggestions);
				List<MarketingPhotoSuggestion> visibleSuggestions = hydrated
					.Where(item => item.Thumbnail != null)
					.DistinctBy(item => FirstFilled(item.OpenverseId, item.Title))
					.Take(MarketingPhotoSuggestionLimit)
					.ToList();
				_marketingPhotoSuggestions.Clear();
				foreach (MarketingPhotoSuggestion suggestion2 in visibleSuggestions)
				{
					_marketingPhotoSuggestions.Add(suggestion2);
				}
				_marketingPhotosLoadedForDate = DateTime.Today;
				MarketingPhotoStatusText.Text = ((_marketingPhotoSuggestions.Count == 0) ? "Não encontrei uma foto elegante nesse tema. Tente cabelo, unhas, spa ou maquiagem." : $"{_marketingPhotoSuggestions.Count} fotos da coleção editorial • {MarketingPhotoThemeLabel(curatedCategory, dailyTheme.Label)}.");
			}
			catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is JsonException) ? 1 : 0) != 0)
			{
				MarketingPhotoStatusText.Text = "A galeria gratuita está indisponível agora. A arte local continua pronta para usar.";
			}
			finally
			{
				_marketingPhotoSearchRunning = false;
				MarketingPhotoSearchButton.IsEnabled = true;
			}
		}

		private static async Task<MarketingPhotoSuggestion> ResolveMarketingPhotoAsync(MarketingPhotoSuggestion suggestion)
		{
			if (!string.IsNullOrWhiteSpace(suggestion.ImageUrl) || string.IsNullOrWhiteSpace(suggestion.OpenverseId))
			{
				return suggestion;
			}
			try
			{
				using (HttpResponseMessage response = await OpenverseClient.GetAsync("https://api.openverse.org/v1/images/" + suggestion.OpenverseId + "/"))
				{
					response.EnsureSuccessStatusCode();
					MarketingPhotoSuggestion result;
					await using (Stream stream = await response.Content.ReadAsStreamAsync())
					{
						using JsonDocument document = await JsonDocument.ParseAsync(stream);
						result = suggestion with
						{
							ImageUrl = JsonText(document.RootElement, "url"),
							LandingUrl = FirstFilled(JsonText(document.RootElement, "foreign_landing_url"), suggestion.LandingUrl),
							LicenseUrl = FirstFilled(JsonText(document.RootElement, "license_url"), suggestion.LicenseUrl)
						};
					}
					return result;
				}
				IL_0374:
				MarketingPhotoSuggestion result2;
				return result2;
			}
			catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is JsonException) ? 1 : 0) != 0)
			{
				return suggestion;
			}
		}

		private static Task<BitmapImage> DownloadMarketingPhotoAsync(string imageUrl, string thumbnailUrl)
		{
			return DownloadMarketingBitmapAsync(new _003C_003Ez__ReadOnlyArray<string>(new string[2] { imageUrl, thumbnailUrl }), 1600);
		}

		private static List<MarketingPhotoSuggestion> BuildCuratedMarketingSuggestions(string category)
		{
			(string Path, string Title)[] assets = category switch
			{
				"hair" => new[]
				{
					("Assets/marketing-campaign-hair.png", "Cabelo em destaque"),
					("Assets/marketing-site-hero-hair.png", "Beleza e movimento")
				},
				"nails" => new[]
				{
					("Assets/marketing-editorial-nails-nude.png", "Manicure nude"),
					("Assets/marketing-editorial-nails-french.png", "Francesinha elegante"),
					("Assets/marketing-editorial-nails-terracotta.png", "Unhas terracota"),
					("Assets/marketing-editorial-nails-service.png", "Atendimento de manicure"),
					("Assets/marketing-campaign-nails.png", "Unhas em destaque")
				},
				"spa" => new[]
				{
					("Assets/marketing-campaign-spa.png", "Ritual de spa")
				},
				"makeup" or "aesthetics" => new[]
				{
					("Assets/marketing-site-overview-makeup.png", "Beleza editorial")
				},
				_ => new[]
				{
					("Assets/marketing-campaign-hair.png", "Beleza em destaque"),
					("Assets/marketing-site-hero-hair.png", "Cabelo com movimento"),
					("Assets/marketing-editorial-nails-nude.png", "Manicure nude"),
					("Assets/marketing-editorial-nails-french.png", "Francesinha elegante"),
					("Assets/marketing-editorial-nails-terracotta.png", "Unhas terracota"),
					("Assets/marketing-editorial-nails-service.png", "Atendimento de manicure"),
					("Assets/marketing-campaign-nails.png", "Detalhes delicados"),
					("Assets/marketing-campaign-spa.png", "Momento de autocuidado"),
					("Assets/marketing-site-overview-makeup.png", "Beleza editorial")
				}
			};

			List<MarketingPhotoSuggestion> suggestions = assets
				.Select(asset => new MarketingPhotoSuggestion(
					asset.Title,
					"Agenda Livre",
					"",
					"",
					"",
					"",
					"",
					LoadMarketingSiteBitmap(asset.Path),
					"",
					category))
				.Where(item => item.Thumbnail != null)
				.ToList();

			IEnumerable<MarketingCuratedPhotoSeed> remoteSeeds = CuratedMarketingPhotoSeeds;
			if (category is not "beauty")
			{
				remoteSeeds = remoteSeeds.Where(seed => seed.Category == category);
			}
			suggestions.AddRange(remoteSeeds.Select(seed => new MarketingPhotoSuggestion(
				seed.Title,
				seed.Creator,
				"cc0",
				"https://creativecommons.org/publicdomain/zero/1.0/",
				"",
				$"https://api.openverse.org/v1/images/{seed.OpenverseId}/thumb/",
				$"https://openverse.org/image/{seed.OpenverseId}",
				null,
				seed.OpenverseId,
				seed.Category)));

			return suggestions
				.DistinctBy(item => FirstFilled(item.OpenverseId, item.Title))
				.Take(MarketingPhotoSuggestionLimit)
				.ToList();
		}

		private sealed record MarketingCuratedPhotoSeed(string Category, string OpenverseId, string Title, string Creator);

		private static readonly MarketingCuratedPhotoSeed[] CuratedMarketingPhotoSeeds =
		{
			new("hair", "0c3e106c-d78b-4443-b031-25ffe408c499", "Trança vista de cima", "Candace McDaniel"),
			new("hair", "2a3d5efe-cf9d-4f9e-be3c-7b5b18ca9ded", "Tranças em destaque", "Candace McDaniel"),
			new("hair", "851966fb-6fec-49f9-b55f-94b92048eef9", "Penteado com trança", "Candace McDaniel"),
			new("hair", "a533fe9a-b16a-459a-8168-b3a793e8dcbe", "Cabelo finalizado", "Candace McDaniel"),
			new("hair", "14ed2770-f410-4e54-b5e1-ff474eeb5c47", "Beleza e cabelo", "Authentic Stock"),
			new("makeup", "6046f5af-4b20-41d5-aa49-010b87beb80c", "Maquiagem no espelho", "Candace McDaniel"),
			new("makeup", "bddc68cd-5196-492b-abc8-2bb51729f796", "Paleta de batons", "Valeria Boltneva"),
			new("makeup", "c15a1f51-0b44-4c43-b81d-b70a9ff09310", "Maquiagem elegante", "Matt Bango"),
			new("aesthetics", "10db4411-a4ed-4e93-9fef-5155c01198b1", "Skincare", "Authentic Stock"),
			new("aesthetics", "64e00096-d4f5-4d28-a550-44103634f1cb", "Pele e autocuidado", "Authentic Stock"),
			new("aesthetics", "9d30d890-17b5-40ae-b82d-fca77b872736", "Rotina de cuidados", "Authentic Stock"),
			new("aesthetics", "b1c2c9ce-6959-4575-899b-bb5c8cd89ec5", "Tratamento facial", "Authentic Stock"),
			new("aesthetics", "695a89f0-3af4-404d-bb22-60c95a250d4b", "Beleza natural", "Authentic Stock"),
			new("spa", "62b4b5fc-64ad-45cf-afc8-89112ca2e20d", "Ritual para as mãos", "Healthy Living"),
			new("spa", "6da3e69e-72e0-48ec-8848-4f5c04f3c42f", "Spa com flores e pedras", "Healthy Living"),
			new("spa", "86e31c24-310d-4dae-b74a-99807c7e4eae", "Banho relaxante", "Authentic Stock"),
			new("spa", "971fff82-0e75-4d34-a2a1-b84ea075dff2", "Conceito de spa", "Healthy Living"),
			new("spa", "b00f3183-d51e-4e1d-a7c2-52fcbed472e0", "Momento de relaxamento", "Matt Bango")
		};

		private static async Task<IReadOnlyList<MarketingPhotoSuggestion>> HydrateMarketingPhotoSuggestionsAsync(IEnumerable<MarketingPhotoSuggestion> suggestions)
		{
			return await Task.WhenAll(suggestions.Take(MarketingPhotoSuggestionLimit).Select(async delegate(MarketingPhotoSuggestion suggestion)
			{
				if (suggestion.Thumbnail != null)
				{
					return suggestion;
				}
				try
				{
					return suggestion with { Thumbnail = await DownloadMarketingBitmapAsync(new[] { suggestion.ThumbnailUrl }, 360) };
				}
				catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is InvalidOperationException || ex is IOException || ex is NotSupportedException || ex is FormatException) ? 1 : 0) != 0)
				{
					return suggestion;
				}
			}));
		}

		private static string? CuratedMarketingCategory(string query)
		{
			string lookup = NormalizeTemplateLookup(query);
			if (lookup.Contains("CABELO", StringComparison.Ordinal) || lookup.Contains("HAIR", StringComparison.Ordinal) || lookup.Contains("BARBEARIA", StringComparison.Ordinal))
			{
				return "hair";
			}
			if (lookup.Contains("UNHA", StringComparison.Ordinal) || lookup.Contains("NAIL", StringComparison.Ordinal) || lookup.Contains("MANICURE", StringComparison.Ordinal))
			{
				return "nails";
			}
			if (lookup.Contains("MAQUIAGEM", StringComparison.Ordinal) || lookup.Contains("MAKEUP", StringComparison.Ordinal))
			{
				return "makeup";
			}
			if (lookup.Contains("ESTETICA", StringComparison.Ordinal) || lookup.Contains("PELE", StringComparison.Ordinal) || lookup.Contains("SKIN", StringComparison.Ordinal) || lookup.Contains("FACIAL", StringComparison.Ordinal))
			{
				return "aesthetics";
			}
			if (lookup.Contains("SPA", StringComparison.Ordinal) || lookup.Contains("MASSAGEM", StringComparison.Ordinal) || lookup.Contains("WELLNESS", StringComparison.Ordinal) || lookup.Contains("BEM ESTAR", StringComparison.Ordinal))
			{
				return "spa";
			}
			if (lookup.Contains("BELEZA", StringComparison.Ordinal) || lookup.Contains("BEAUTY", StringComparison.Ordinal) || lookup.Contains("AUTOCUIDADO", StringComparison.Ordinal))
			{
				return "beauty";
			}
			return null;
		}

		private void UpdateMarketingPhotoThemeSelection(string? category)
		{
			if (MarketingPhotoHairThemeTab == null || MarketingPhotoNailsThemeTab == null || MarketingPhotoAestheticsThemeTab == null || MarketingPhotoSpaThemeTab == null || MarketingPhotoMakeupThemeTab == null)
			{
				return;
			}
			_marketingPhotoThemeSyncing = true;
			try
			{
				MarketingPhotoHairThemeTab.IsChecked = category == "hair";
				MarketingPhotoNailsThemeTab.IsChecked = category == "nails";
				MarketingPhotoAestheticsThemeTab.IsChecked = category == "aesthetics";
				MarketingPhotoSpaThemeTab.IsChecked = category == "spa";
				MarketingPhotoMakeupThemeTab.IsChecked = category == "makeup";
			}
			finally
			{
				_marketingPhotoThemeSyncing = false;
			}
		}

		private static readonly HttpClient OpenverseClient = new()
		{
			Timeout = TimeSpan.FromSeconds(15)
		};

		private static string JsonText(JsonElement element, string propertyName)
		{
			if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
			{
				return "";
			}
			return property.GetString()?.Trim() ?? "";
		}

		private static decimal ReportPercent(int value, int total)
		{
			if (total <= 0)
			{
				return 0m;
			}
			return (decimal)value * 100m / (decimal)total;
		}

		private RenderTargetBitmap RenderMarketingStudioArtwork()
		{
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			FrameworkElement artwork = (_marketingEditorInitialized && MarketingEditorPreviewCard != null)
				? MarketingEditorPreviewCard
				: MarketingStudioPreviewCard;
			SetMarketingEditorSelectionChrome(false);
			artwork.UpdateLayout();
			int width = ((_marketingStudioChannel == "post") ? 1080 : 1080);
			int height = ((_marketingStudioChannel == "post") ? 1080 : 1920);
			DrawingVisual visual = new DrawingVisual();
			using (DrawingContext drawing = visual.RenderOpen())
			{
				drawing.DrawRectangle(new VisualBrush(artwork), null, new Rect(0.0, 0.0, (double)width, (double)height));
			}
			RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
			renderTargetBitmap.Render(visual);
			SetMarketingEditorSelectionChrome(true);
			return renderTargetBitmap;
		}

		private void RestoreFinanceFunnelFocus()
		{
			foreach (Polygon item in FinanceFunnelShapes())
			{
				FinanceHoverDetail detail;
				decimal rawValue = ((item.Tag is string key && _financeFunnelHoverDetails.TryGetValue(key, out detail)) ? detail.RawValue : 0m);
				item.Opacity = ((rawValue > 0m) ? 1.0 : 0.16);
				item.Stroke = null;
				item.StrokeThickness = 0.0;
				Panel.SetZIndex(item, 0);
			}
		}

		private void ScrollAppointmentTimeline(double delta)
		{
			double target = Math.Clamp(AppointmentTimelineScrollViewer.HorizontalOffset + delta, 0.0, AppointmentTimelineScrollViewer.ScrollableWidth);
			AppointmentTimelineScrollViewer.ScrollToHorizontalOffset(target);
		}

		private List<string> SelectedMarketingSlots()
		{
			if (_marketingEditorInitialized && MarketingEditorSlot1Check != null)
			{
				return SelectedMarketingEditorSlots();
			}
			if (MarketingStudioSlot1Check != null)
			{
				return (from item in new CheckBox[5] { MarketingStudioSlot1Check, MarketingStudioSlot2Check, MarketingStudioSlot3Check, MarketingStudioSlot4Check, MarketingStudioSlot5Check }
					where item.Visibility == Visibility.Visible && item.IsChecked == true
					select item.Content?.ToString() ?? "" into item
					where !string.IsNullOrWhiteSpace(item)
					select item).ToList();
			}
			if (MarketingSlot1Check == null)
			{
				return new List<string>();
			}
			return (from item in new CheckBox[5] { MarketingSlot1Check, MarketingSlot2Check, MarketingSlot3Check, MarketingSlot4Check, MarketingSlot5Check }
				where item.Visibility == Visibility.Visible && item.IsChecked == true
				select item.Content?.ToString() ?? "" into item
				where !string.IsNullOrWhiteSpace(item)
				select item).ToList();
		}

		private DateTime ShiftHomePeriod(DateTime date, int direction)
		{
			return _homePeriodModeIndex switch
			{
				1 => date.AddDays(7 * direction), 
				2 => date.AddMonths(direction), 
				_ => date.AddDays(direction), 
			};
		}

		private void StopAppointmentTimelineDrag()
		{
			_appointmentTimelineDragging = false;
			AppointmentTimelineScrollViewer.Cursor = Cursors.Hand;
			if (AppointmentTimelineScrollViewer.IsMouseCaptured)
			{
				AppointmentTimelineScrollViewer.ReleaseMouseCapture();
			}
		}

		private void ToggleCustomWindowState()
		{
			base.WindowState = ((base.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
		}

		private void UpdateMarketingStudioPreview()
		{
			if (MarketingStudioPreviewTitleText != null && MarketingStudioTitleTextBox != null)
			{
				MarketingStudioBusinessIdentity();
				string title = MarketingStudioTitleValue();
				MarketingStudioPreviewTitleText.Text = title.ToUpperInvariant();
				MarketingStudioPreviewCopyText.Text = MarketingStudioCopyValue();
				List<string> slots = SelectedMarketingSlots();
				if (MarketingStudioSelectedSlotsText != null)
				{
					TextBlock marketingStudioSelectedSlotsText = MarketingStudioSelectedSlotsText;
					marketingStudioSelectedSlotsText.Text = slots.Count switch
					{
						0 => "Nenhum selecionado", 
						1 => "1 selecionado", 
						_ => $"{slots.Count} selecionados", 
					};
				}
				MarketingStudioPreviewSlotsText.Text = ((slots.Count == 0) ? "Selecione os horários" : string.Join(Environment.NewLine, from chunk in slots.Chunk(2)
					select string.Join("  •  ", chunk)));
				if (_marketingEditorInitialized && !_marketingEditorApplying)
				{
					_marketingEditorApplying = true;
					try
					{
						MarketingEditorCampaignTitleTextBox.Text = MarketingStudioTitleTextBox.Text;
						MarketingEditorCampaignCopyTextBox.Text = MarketingStudioCopyTextBox.Text;
						MarketingEditorTitleText.Text = MarketingStudioPreviewTitleText.Text;
						MarketingEditorCopyText.Text = MarketingStudioPreviewCopyText.Text;
						MarketingEditorUpdateSlots();
					}
					finally
					{
						_marketingEditorApplying = false;
					}
				}
			}
		}

		private void UpdateReportsFunnelFocus(string stage)
		{
			int scheduledGap = Math.Max(0, _reportsFunnelScheduled - _reportsFunnelConfirmed);
			int confirmedGap = Math.Max(0, _reportsFunnelConfirmed - _reportsFunnelAttended);
			int attendedGap = Math.Max(0, _reportsFunnelAttended - _reportsFunnelReceived);
			string title;
			string formula;
			string gap;
			string impact;
			switch (stage)
			{
			case "scheduled":
				title = "Agendados";
				formula = $"{_reportsFunnelScheduled:N0} registros válidos no período";
				gap = "Base do funil: todos os agendamentos, exceto bloqueios";
				impact = "Fonte: agenda sincronizada";
				break;
			case "confirmed":
				title = "Confirmados";
				formula = $"{_reportsFunnelConfirmed:N0} ÷ {_reportsFunnelScheduled:N0} = {ReportPercent(_reportsFunnelConfirmed, _reportsFunnelScheduled):N0}%";
				gap = $"Sem avanço registrado: {scheduledGap:N0}";
				impact = "Valor associado: " + _reportsFunnelScheduledGapValue.ToString("C0", Brazil);
				break;
			case "received":
				title = "Recebidos";
				formula = $"{_reportsFunnelReceived:N0} ÷ {_reportsFunnelScheduled:N0} = {ReportPercent(_reportsFunnelReceived, _reportsFunnelScheduled):N0}%";
				gap = $"Presenças sem recebimento confirmado: {attendedGap:N0}";
				impact = "Valor em aberto: " + _reportsFunnelAttendedGapValue.ToString("C0", Brazil);
				break;
			default:
				title = "Compareceram";
				formula = $"{_reportsFunnelAttended:N0} ÷ {_reportsFunnelConfirmed:N0} = {ReportPercent(_reportsFunnelAttended, _reportsFunnelConfirmed):N0}%";
				gap = $"Confirmados sem presença registrada: {confirmedGap:N0}";
				impact = "Valor associado: " + _reportsFunnelConfirmedGapValue.ToString("C0", Brazil);
				stage = "attended";
				break;
			}
			ReportsFunnelDetailTitleText.Text = title;
			ReportsFunnelDetailFormulaText.Text = formula;
			ReportsFunnelDetailGapText.Text = gap;
			ReportsFunnelDetailImpactText.Text = impact;
			ReportsFunnelScheduledBorder.BorderThickness = new Thickness((!(stage == "scheduled")) ? 1 : 2);
			ReportsFunnelConfirmedBorder.BorderThickness = new Thickness((!(stage == "confirmed")) ? 1 : 2);
			ReportsFunnelAttendedBorder.BorderThickness = new Thickness((stage == "attended") ? 2.0 : 1.5);
			ReportsFunnelReceivedBorder.BorderThickness = new Thickness((!(stage == "received")) ? 1 : 2);
			ReportsFunnelScheduledBorder.BorderBrush = ((stage == "scheduled") ? InkBrush : AccentBrush);
			ReportsFunnelConfirmedBorder.BorderBrush = ((stage == "confirmed") ? InkBrush : Solid("#FF8B4D"));
			ReportsFunnelAttendedBorder.BorderBrush = AccentBrush;
			ReportsFunnelReceivedBorder.BorderBrush = ((stage == "received") ? AccentBrush : Solid("#F5D7C5"));
		}

		private void UpdateStoryPreview()
		{
			if (StoryPreviewTitleText != null && StoryTitleTextBox != null)
			{
				StoryPreviewTitleText.Text = (string.IsNullOrWhiteSpace(StoryTitleTextBox.Text) ? "HORÁRIOS LIVRES HOJE" : StoryTitleTextBox.Text.Trim());
				StoryPreviewSubtitleText.Text = (string.IsNullOrWhiteSpace(StorySubtitleTextBox.Text) ? "Reserve seu momento ✨" : StorySubtitleTextBox.Text.Trim());
				StoryPreviewTitleText.FontSize = StoryTitleSizeSlider?.Value ?? 29.0;
				List<string> slots = SelectedMarketingSlots();
				StoryPreviewSlotsText.Text = ((slots.Count == 0) ? "Selecione os horários" : string.Join(Environment.NewLine, from chunk in slots.Chunk(2)
					select string.Join("  •  ", chunk)));
			}
		}

		private DateTime AppointmentAssistantPaymentPromptAt(Appointment appointment, out bool learnedTiming)
		{
			int scheduledMinutes = ((appointment.DurationMinutes > 0) ? Math.Clamp(appointment.DurationMinutes, 5, 480) : 15);
			List<(Appointment Appointment, double ResidualMinutes)> samples = (from item in (from item in _data.Appointments
					where item.Id != appointment.Id && item.Status == AppointmentStatus.Done && item.Start < appointment.Start && !IsPreviewAppointment(item)
					orderby item.Start descending
					select item).Take(80).Select(delegate(Appointment item)
				{
					double totalMinutes = ((item.PaymentConfirmedAt ?? item.UpdatedAt) - item.Start).TotalMinutes;
					int num = ((item.DurationMinutes > 0) ? Math.Clamp(item.DurationMinutes, 5, 480) : 15);
					return (Appointment: item, ActualMinutes: totalMinutes, ResidualMinutes: totalMinutes - (double)num);
				}).Where(delegate((Appointment Appointment, double ActualMinutes, double ResidualMinutes) item)
				{
					double item2 = item.ActualMinutes;
					return item2 >= 5.0 && item2 <= 480.0;
				})
				select (Appointment: item.Appointment, ResidualMinutes: item.ResidualMinutes)).ToList();
			List<(Appointment Appointment, double ResidualMinutes)> selectedSamples = PickSamples((Appointment item) => AppointmentAssistantSameService(item, appointment) && AppointmentAssistantSameProfessional(item, appointment), 3);
			if (selectedSamples.Count == 0)
			{
				selectedSamples = PickSamples((Appointment item) => AppointmentAssistantSameService(item, appointment), 3);
			}
			if (selectedSamples.Count == 0 && !string.IsNullOrWhiteSpace(appointment.Segment))
			{
				selectedSamples = PickSamples((Appointment item) => string.Equals(item.Segment, appointment.Segment, StringComparison.OrdinalIgnoreCase), 5);
			}
			if (selectedSamples.Count == 0 && samples.Count >= 8)
			{
				selectedSamples = samples.Take(24).ToList();
			}
			learnedTiming = selectedSamples.Count > 0;
			if (!learnedTiming)
			{
				return appointment.Start.AddMinutes(scheduledMinutes);
			}
			List<double> orderedResidualMinutes = (from item in selectedSamples
				select item.ResidualMinutes into value
				orderby value
				select value).ToList();
			int middle = orderedResidualMinutes.Count / 2;
			double medianResidual = ((orderedResidualMinutes.Count % 2 == 0) ? ((orderedResidualMinutes[middle - 1] + orderedResidualMinutes[middle]) / 2.0) : orderedResidualMinutes[middle]);
			double learnedDelay = Math.Clamp(Math.Max(0.0, medianResidual), 0.0, 120.0);
			double roundedMinutes = Math.Ceiling(Math.Clamp((double)scheduledMinutes + learnedDelay, 5.0, 480.0) / 5.0) * 5.0;
			return appointment.Start.AddMinutes(roundedMinutes);
			List<(Appointment Appointment, double ResidualMinutes)> PickSamples(Func<Appointment, bool> predicate, int minimum)
			{
				List<(Appointment Appointment, double ResidualMinutes)> matches = samples.Where(((Appointment Appointment, double ResidualMinutes) item) => predicate(item.Appointment)).Take(24).ToList();
				if (matches.Count < minimum)
				{
					return new List<(Appointment Appointment, double ResidualMinutes)>();
				}
				return matches;
			}
		}

		private static async Task<BitmapImage> DownloadMarketingBitmapAsync(IEnumerable<string> candidates, int decodePixelWidth)
		{
			Exception lastException = null;
			foreach (string candidate in candidates.Where((string value) => !string.IsNullOrWhiteSpace(value)).Distinct())
			{
				try
				{
					using (HttpResponseMessage response = await OpenverseClient.GetAsync(candidate, HttpCompletionOption.ResponseHeadersRead))
					{
						response.EnsureSuccessStatusCode();
						BitmapImage result;
						await using (Stream source = await response.Content.ReadAsStreamAsync())
						{
							using MemoryStream memory = new MemoryStream();
							await source.CopyToAsync(memory);
							memory.Position = 0L;
							BitmapImage bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.DecodePixelWidth = decodePixelWidth;
							bitmap.StreamSource = memory;
							bitmap.EndInit();
							((Freezable)bitmap).Freeze();
							result = bitmap;
						}
						return result;
					}
					IL_0346:;
				}
				catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is IOException || ex is NotSupportedException || ex is FormatException) ? 1 : 0) != 0)
				{
					lastException = ex;
				}
			}
			throw new InvalidOperationException("A imagem selecionada não pôde ser carregada.", lastException);
		}

		private static bool AppointmentAssistantSameService(Appointment left, Appointment right)
		{
			if (!string.IsNullOrWhiteSpace(left.ServiceId) && !string.IsNullOrWhiteSpace(right.ServiceId))
			{
				return left.ServiceId.Equals(right.ServiceId, StringComparison.OrdinalIgnoreCase);
			}
			if (!string.IsNullOrWhiteSpace(left.ServiceName))
			{
				return left.ServiceName.Equals(right.ServiceName, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		private static bool AppointmentAssistantSameProfessional(Appointment left, Appointment right)
		{
			if (!string.IsNullOrWhiteSpace(left.ProfessionalId) && !string.IsNullOrWhiteSpace(right.ProfessionalId))
			{
				return left.ProfessionalId.Equals(right.ProfessionalId, StringComparison.OrdinalIgnoreCase);
			}
			if (!string.IsNullOrWhiteSpace(left.ProfessionalName))
			{
				return left.ProfessionalName.Equals(right.ProfessionalName, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		private void AgendaBoardToolbar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			if (AgendaBoardDatePanel != null && AgendaBoardRangeText != null)
			{
				Border agendaBoardDatePanel = AgendaBoardDatePanel;
				Size newSize = e.NewSize;
				agendaBoardDatePanel.Visibility = ((!(newSize.Width >= 650.0)) ? Visibility.Collapsed : Visibility.Visible);
				TextBlock agendaBoardRangeText = AgendaBoardRangeText;
				newSize = e.NewSize;
				agendaBoardRangeText.Visibility = ((!(newSize.Width >= 780.0)) ? Visibility.Collapsed : Visibility.Visible);
			}
		}

		private void AgendaFlowViewAllButton_Click(object sender, RoutedEventArgs e)
		{
			_agendaModeIndex = 1;
			UpdateAgendaModeButtons();
			ShowStatus("Lista completa da agenda aberta.");
		}

		private void AppointmentAssistantActionButton_Click(object sender, RoutedEventArgs e)
		{
			AppointmentAssistantRow row = _appointmentAssistantCurrent;
			if ((object)row == null)
			{
				RefreshAppointmentAssistant();
				return;
			}
			Appointment appointment = _data.Appointments.FirstOrDefault((Appointment item) => item.Id == row.Appointment.Id);
			if (appointment == null)
			{
				RefreshAppointmentAssistant();
			}
			else if (row.State == AppointmentAssistantState.AttendanceCheck)
			{
				ConfirmAppointmentAttendance(appointment);
			}
			else if (row.State != AppointmentAssistantState.Payment)
			{
				OpenAppointmentFromAssistant(appointment);
			}
			else
			{
				ShowAppointmentInfoPopup(AgendaWorkspaceView, appointment);
			}
		}

		private void AppointmentDatePicker_Loaded(object sender, RoutedEventArgs e)
		{
			if (!(sender is DatePicker datePicker))
			{
				return;
			}
			datePicker.ApplyTemplate();
			if (datePicker.Template.FindName("PART_TextBox", datePicker) is DatePickerTextBox textBox && _appointmentDateTextBox != textBox)
			{
				if (_appointmentDateTextBox != null)
				{
					_appointmentDateTextBox.TextChanged -= AppointmentDateTextBox_TextChanged;
					_appointmentDateTextBox.PreviewKeyDown -= AppointmentDateTextBox_PreviewKeyDown;
				}
				_appointmentDateTextBox = textBox;
				textBox.TextChanged += AppointmentDateTextBox_TextChanged;
				textBox.PreviewKeyDown += AppointmentDateTextBox_PreviewKeyDown;
				FormatAppointmentDateText(textBox);
			}
		}

		private void AppointmentFlowActionButton_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is Button { Tag: var tag } button))
			{
				return;
			}
			string appointmentId = tag as string;
			if (appointmentId == null)
			{
				return;
			}
			Appointment appointment = _data.Appointments.FirstOrDefault((Appointment item) => item.Id == appointmentId);
			if (appointment == null)
			{
				RefreshAll();
				ShowStatus("O atendimento não está mais disponível.");
				return;
			}
			string text = button.CommandParameter as string;
			if (!(text == "arrival"))
			{
				if (text == "payment")
				{
					_selectedAppointment = appointment;
					ShowAppointmentInfoPopup(AgendaWorkspaceView, appointment);
				}
			}
			else
			{
				ConfirmAppointmentAttendance(appointment);
			}
		}

		private void AppointmentPaymentOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.OriginalSource == AppointmentPaymentOverlay)
			{
				CloseAppointmentInfoPopup();
				e.Handled = true;
			}
		}

		private void AppointmentTimelineScrollLeftButton_Click(object sender, RoutedEventArgs e)
		{
			ScrollAppointmentTimeline(-180.0);
		}

		private void AppointmentTimelineScrollRightButton_Click(object sender, RoutedEventArgs e)
		{
			ScrollAppointmentTimeline(180.0);
		}

		private void AppointmentTimelineScrollViewer_LostMouseCapture(object sender, MouseEventArgs e)
		{
			StopAppointmentTimelineDrag();
		}

		private void AppointmentTimelineScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			_appointmentTimelineDragging = true;
			_appointmentTimelineDragStart = e.GetPosition(AppointmentTimelineScrollViewer);
			_appointmentTimelineDragStartOffset = AppointmentTimelineScrollViewer.HorizontalOffset;
			AppointmentTimelineScrollViewer.Cursor = Cursors.SizeWE;
			AppointmentTimelineScrollViewer.CaptureMouse();
			e.Handled = true;
		}

		private void AppointmentTimelineScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			StopAppointmentTimelineDrag();
			e.Handled = true;
		}

		private void AppointmentTimelineScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			if (_appointmentTimelineDragging && e.LeftButton == MouseButtonState.Pressed)
			{
				Point current = e.GetPosition(AppointmentTimelineScrollViewer);
				double delta = current.X - _appointmentTimelineDragStart.X;
				AppointmentTimelineScrollViewer.ScrollToHorizontalOffset(Math.Clamp(_appointmentTimelineDragStartOffset - delta, 0.0, AppointmentTimelineScrollViewer.ScrollableWidth));
				e.Handled = true;
			}
		}

		private void AppointmentTimelineScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			ScrollAppointmentTimeline((e.Delta > 0) ? (-90) : 90);
			e.Handled = true;
		}

		private void CloseStoryCreatorButton_Click(object sender, RoutedEventArgs e)
		{
			StoryCreatorOverlay.Visibility = Visibility.Collapsed;
		}

		private void ConfirmAppointmentPayment(Appointment appointment, string paymentMethod, string paymentProvider = "", string paymentReference = "", string paymentStatus = "approved")
		{
			Appointment currentAppointment = _data.Appointments.FirstOrDefault((Appointment item) => item.Id == appointment.Id);
			if (currentAppointment == null)
			{
				RefreshAll();
				ShowStatus("O agendamento não está mais disponível para confirmação.");
				return;
			}
			appointment = currentAppointment;
			AppointmentStatus status = appointment.Status;
			bool flag = (uint)(status - 5) <= 2u;
			if (flag || appointment.PaymentConfirmedAt.HasValue || HasCustomerReceivableForAppointment(appointment.Id))
			{
				RefreshAll();
				ShowStatus("Esse atendimento já foi encerrado sem cobrança ou já possui um pagamento registrado.");
				return;
			}
			DateTime confirmedAt = DateTime.Now;
			appointment.Status = AppointmentStatus.Done;
			appointment.PaymentConfirmedAt = confirmedAt;
			appointment.PaymentMethod = paymentMethod;
			appointment.PaymentProvider = paymentProvider;
			appointment.PaymentReference = paymentReference;
			appointment.PaymentStatus = paymentStatus;
			appointment.UpdatedAt = confirmedAt;
			_store.Save(_data);
			RefreshAll();
			string message = ((appointment.Price > 0m) ? $"Pagamento de {appointment.Price.ToString("C", Brazil)} confirmado em {paymentMethod}." : "Atendimento finalizado sem cobrança.");
			ShowStatus(message + " Valor catalogado no Financeiro sem duplicação.");
		}

		private void CopyStoryCaptionButton_Click(object sender, RoutedEventArgs e)
		{
			List<string> slots = SelectedMarketingSlots();
			Clipboard.SetText($"✨ {StoryTitleTextBox.Text}\n{StorySubtitleTextBox.Text}\n\nHorários: {string.Join(", ", slots)}\nChame o {BusinessDisplayName()} e reserve o seu!\n\n#agenda #beleza #horariosdisponiveis");
			ShowStatus("Legenda do story copiada.");
		}

		private void CustomCloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void CustomMaximizeButton_Click(object sender, RoutedEventArgs e)
		{
			ToggleCustomWindowState();
		}

		private void CustomMinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			base.WindowState = WindowState.Minimized;
		}

		private void CustomTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
			{
				return;
			}
			if (e.ClickCount == 2)
			{
				ToggleCustomWindowState();
				return;
			}
			try
			{
				DragMove();
			}
			catch (InvalidOperationException)
			{
			}
		}

		private void EditEstablishmentRevenueGoalButton_Click(object sender, RoutedEventArgs e)
		{
			(Window Dialog, StackPanel Body, TextBlock ErrorText, Button PrimaryButton, Button CancelButton) shell = CreateFinanceEditorDialog("Meta de faturamento", "Defina quanto o estabelecimento pretende faturar por mês.", "Salvar meta", PackIconKind.WalletOutline, useBodyCard: false);
			shell.Dialog.Width = 520.0;
			shell.Dialog.MaxHeight = 420.0;
			AddFinanceDialogSection(shell.Body, PackIconKind.ChartLine, "Meta mensal", "O progresso será calculado com base na receita registrada neste mês.");
			TextBox goalBox = new TextBox
			{
				Text = Math.Max(0m, _data.Settings.MonthlyRevenueGoal).ToString("N2", Brazil),
				Style = (Style)FindResource("MaterialDesignOutlinedTextBox"),
				Height = 48.0,
				FontSize = 15.0,
				Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
			};
			HintAssist.SetHint((DependencyObject)(object)goalBox, "Meta mensal em reais");
			shell.Body.Children.Add(goalBox);
			shell.PrimaryButton.Click += delegate
			{
				if (!TryParseMoney(goalBox.Text, out var value) || value <= 0m)
				{
					SetDialogError(shell.ErrorText, "Informe uma meta maior que zero.");
					goalBox.Focus();
					goalBox.SelectAll();
				}
				else
				{
					_data.Settings.MonthlyRevenueGoal = value;
					_store.Save(_data);
					RefreshEstablishmentPage();
					shell.Dialog.DialogResult = true;
				}
			};
			goalBox.SelectAll();
			goalBox.Focus();
			ShowAppDialog(shell.Dialog);
		}

		private void ExportMarketingStudioPngButton_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog dialog = new SaveFileDialog
			{
				Title = "Exportar publicação",
				Filter = "Imagem PNG (*.png)|*.png",
				DefaultExt = ".png",
				AddExtension = true,
				FileName = $"publicacao-{_marketingStudioChannel}-{DateTime.Now:yyyyMMdd-HHmm}.png"
			};
			if (dialog.ShowDialog(this) != true)
			{
				return;
			}
			try
			{
				SaveMarketingStudioArtwork(dialog.FileName);
				ShowStatus("Publicação exportada: " + System.IO.Path.GetFileName(dialog.FileName));
			}
			catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException) ? 1 : 0) != 0)
			{
				MessageBox.Show(this, "Não foi possível exportar a publicação.\n\n" + ex.Message, "Marketing", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void SaveMarketingStudioArtwork(string path)
		{
			PngBitmapEncoder encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(RenderMarketingStudioArtwork()));
			using FileStream stream = File.Create(path);
			encoder.Save(stream);
		}

		private void ExportStoryButton_Click(object sender, RoutedEventArgs e)
		{
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			SaveFileDialog dialog = new SaveFileDialog
			{
				Title = "Exportar story",
				Filter = "Imagem PNG (*.png)|*.png",
				DefaultExt = ".png",
				AddExtension = true,
				FileName = $"story-horarios-{DateTime.Today:yyyy-MM-dd}.png"
			};
			if (dialog.ShowDialog(this) != true)
			{
				return;
			}
			try
			{
				StoryPreviewCard.UpdateLayout();
				DrawingVisual visual = new DrawingVisual();
				using (DrawingContext drawing = visual.RenderOpen())
				{
					drawing.DrawRectangle(new VisualBrush(StoryPreviewCard), null, new Rect(0.0, 0.0, 1080.0, 1920.0));
				}
				RenderTargetBitmap bitmap = new RenderTargetBitmap(1080, 1920, 96.0, 96.0, PixelFormats.Pbgra32);
				bitmap.Render(visual);
				PngBitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bitmap));
				using FileStream stream = File.Create(dialog.FileName);
				encoder.Save(stream);
				ShowStatus("Story exportado: " + System.IO.Path.GetFileName(dialog.FileName));
			}
			catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException) ? 1 : 0) != 0)
			{
				MessageBox.Show(this, "Não foi possível exportar o story.\n\n" + ex.Message, "Criador de stories", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void FinanceForecastCanvas_MouseLeave(object sender, MouseEventArgs e)
		{
			FinanceForecastHoverLine.Visibility = Visibility.Collapsed;
			if (_financeForecastHoverToolTip != null)
			{
				_financeForecastHoverToolTip.IsOpen = false;
			}
		}

		private void FinanceForecastCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			if (_financeForecastPotentialResults.Count == 12 && _financeForecastConfirmedResults.Count == 12 && _financeForecastCumulativeExpenses.Count == 12)
			{
				Point point = e.GetPosition(FinanceForecastCanvas);
				int index = Math.Clamp((int)Math.Round(point.X / 1060.0 * 11.0), 0, 11);
				double x = (double)index * 1060.0 / 11.0;
				FinanceForecastHoverLine.X1 = x;
				FinanceForecastHoverLine.X2 = x;
				FinanceForecastHoverLine.Visibility = Visibility.Visible;
				DateTime start = _financeForecastStartDate.AddDays(index * 7);
				DateTime end = start.AddDays(6.0);
				decimal potential = _financeForecastPotentialResults[index];
				decimal confirmed = _financeForecastConfirmedResults[index];
				decimal expenses = _financeForecastCumulativeExpenses[index];
				FinanceHoverDetail detail = new FinanceHoverDetail($"Semana S{index + 1} • {start:dd/MM}–{end:dd/MM}", potential.ToString("C", Brazil), $"Resultado potencial de {potential:C}; faixa confirmada em {confirmed:C}.", "Resultado atual + agenda acumulada − despesas registradas − comissões configuradas.", "Agenda, despesas e comissões registradas da conta.", $"Confirmado {confirmed:C0} • Despesas {expenses:C0}", potential);
				if (_financeForecastHoverToolTip == null)
				{
					_financeForecastHoverToolTip = new ToolTip
					{
						Background = Brushes.Transparent,
						BorderThickness = new Thickness(0.0),
						Padding = new Thickness(0.0),
						Placement = PlacementMode.MousePoint,
						HorizontalOffset = 14.0,
						VerticalOffset = 12.0,
						HasDropShadow = false,
						StaysOpen = true
					};
				}
				_financeForecastHoverToolTip.PlacementTarget = FinanceForecastCanvas;
				_financeForecastHoverToolTip.Content = CreateFinanceHoverToolTipContent(detail);
				_financeForecastHoverToolTip.IsOpen = true;
			}
		}

		private void FinanceFunnelStage_MouseEnter(object sender, MouseEventArgs e)
		{
			if (!(sender is Polygon { Tag: string key } selected) || !_financeFunnelHoverDetails.TryGetValue(key, out FinanceHoverDetail detail))
			{
				return;
			}
			foreach (Polygon item in FinanceFunnelShapes())
			{
				item.Opacity = ((item == selected) ? 1.0 : 0.12);
				item.Stroke = ((item == selected) ? Brushes.White : null);
				item.StrokeThickness = ((item == selected) ? 2.0 : 0.0);
				Panel.SetZIndex(item, 0);
			}
			FinanceFunnelDetailTitleText.Text = detail.Title + " • " + detail.Value;
			FinanceFunnelDetailDescriptionText.Text = detail.Description;
			FinanceFunnelConversionText.Text = detail.Metric;
		}

		private void FinanceFunnelStage_MouseLeave(object sender, MouseEventArgs e)
		{
			RestoreFinanceFunnelFocus();
			FinanceFunnelDetailTitleText.Text = "Passe o mouse em uma etapa";
			FinanceFunnelDetailDescriptionText.Text = "Veja a regra e a diferença para a etapa anterior.";
			FinanceFunnelConversionText.Text = _financeFunnelDefaultConversionText;
		}

		private void FinancePyramidSegment_MouseEnter(object sender, MouseEventArgs e)
		{
			if (!(sender is Polygon { Tag: string key } selected) || !_financePyramidHoverDetails.ContainsKey(key))
			{
				return;
			}
			foreach (Polygon item in FinancePyramidShapes())
			{
				item.Opacity = ((item == selected) ? 1.0 : 0.12);
				item.Stroke = ((item == selected) ? Brushes.White : null);
				item.StrokeThickness = ((item == selected) ? 2.0 : 0.0);
				Panel.SetZIndex(item, 0);
			}
		}

		private void FinancePyramidSegment_MouseLeave(object sender, MouseEventArgs e)
		{
			foreach (Polygon item in FinancePyramidShapes())
			{
				FinanceHoverDetail detail;
				decimal rawValue = ((item.Tag is string key && _financePyramidHoverDetails.TryGetValue(key, out detail)) ? detail.RawValue : 0m);
				item.Opacity = ((rawValue > 0m) ? 1.0 : 0.16);
				item.Stroke = null;
				item.StrokeThickness = 0.0;
				Panel.SetZIndex(item, 0);
			}
		}

		private void FinanceSimulateScenarioButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshFinancePage();
			FinanceScenarioModeText.Text = "DADOS REAIS";
			FinanceScenarioModeText.Foreground = Solid("#F4A379");
			ShowStatus("Análise atualizada com os registros sincronizados desta conta.");
		}

		private void HomePeriodModeButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button { Tag: string rawTag } && int.TryParse(rawTag, out var selectedIndex))
			{
				_homePeriodModeIndex = Math.Clamp(selectedIndex, 0, 2);
				RefreshAll();
				ShowStatus("Navegação por " + _homePeriodModeIndex switch
				{
					1 => "semana", 
					2 => "mês", 
					_ => "dia", 
				} + " ativada.");
			}
		}

		private void HomeScheduleBoardScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (sender is ScrollViewer viewer)
			{
				double targetOffset = Math.Clamp(viewer.VerticalOffset - (double)e.Delta * 0.72, 0.0, viewer.ScrollableHeight);
				viewer.ScrollToVerticalOffset(targetOffset);
				e.Handled = true;
			}
		}

		private void HomeWeekDayButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button { DataContext: WeekSummaryRow selectedDay })
			{
				_selectedDate = selectedDay.Date;
				RefreshAll();
			}
		}

		private void InitialPhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_formattingInitialPhone || !(sender is TextBox textBox))
			{
				return;
			}
			string original = textBox.Text ?? "";
			int digitCaret = OnlyDigits(original.Substring(0, Math.Min(textBox.CaretIndex, original.Length))).Length;
			string formatted = FormatCustomerPhoneInput(original);
			if (formatted == original)
			{
				return;
			}
			_formattingInitialPhone = true;
			try
			{
				textBox.Text = formatted;
				textBox.CaretIndex = CaretIndexAfterDigits(formatted, Math.Min(digitCaret, 11));
			}
			finally
			{
				_formattingInitialPhone = false;
			}
		}

		private async void MarketingPhotoSearchButton_Click(object sender, RoutedEventArgs e)
		{
			await RefreshMarketingPhotosAsync(MarketingPhotoSearchTextBox.Text);
		}

		private async void MarketingPhotoSearchTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if ((int)e.Key == 6)
			{
				e.Handled = true;
				await RefreshMarketingPhotosAsync(MarketingPhotoSearchTextBox.Text);
			}
		}

		private async void MarketingPhotoThemeButton_Checked(object sender, RoutedEventArgs e)
		{
			if (!_marketingPhotoThemeSyncing && sender is RadioButton { Tag: string query } button)
			{
				MarketingPhotoSearchTextBox.Text = button.Content?.ToString() ?? query;
				await RefreshMarketingPhotosAsync(query);
			}
		}

		private void MarketingStudioChannelButton_Checked(object sender, RoutedEventArgs e)
		{
			if (!(sender is RadioButton { Tag: string channel }))
			{
				return;
			}
			_marketingStudioChannel = channel;
			if (MarketingStudioPreviewHeading != null && MarketingStudioStoryTab != null && MarketingStudioPostTab != null && MarketingStudioWhatsAppTab != null)
			{
				TextBlock marketingStudioPreviewHeading = MarketingStudioPreviewHeading;
				string text = ((channel == "post") ? "Prévia do post" : ((!(channel == "whatsapp")) ? "Prévia do story" : "Prévia do WhatsApp"));
				marketingStudioPreviewHeading.Text = text;
				RadioButton[] array = new RadioButton[3] { MarketingStudioStoryTab, MarketingStudioPostTab, MarketingStudioWhatsAppTab };
				foreach (RadioButton obj in array)
				{
					bool active = string.Equals(obj.Tag?.ToString(), channel, StringComparison.OrdinalIgnoreCase);
					obj.IsChecked = active;
				}
				UpdateMarketingStudioPreview();
			}
		}

		private void MarketingStudioDefaultTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if (textBox != null && HasMarketingStudioDefaultText(textBox))
			{
				((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)delegate
				{
					textBox.SelectAll();
				}, (DispatcherPriority)5, Array.Empty<object>());
			}
		}

		private void MarketingStudioDefaultTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
			{
				textBox.Text = ((textBox == MarketingStudioTitleTextBox) ? "Horários livres" : "Temos horários disponíveis para você. Escolha o melhor momento e faça sua reserva.");
			}
		}

		private void MarketingStudioDefaultTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is TextBox { IsKeyboardFocusWithin: false } textBox && HasMarketingStudioDefaultText(textBox))
			{
				e.Handled = true;
				textBox.Focus();
				textBox.SelectAll();
			}
		}

		private void MarketingStudioEditor_Changed(object sender, TextChangedEventArgs e)
		{
			UpdateMarketingStudioPreview();
		}

		private void MarketingStudioSlot_Changed(object sender, RoutedEventArgs e)
		{
			UpdateMarketingStudioPreview();
		}

		private void NextHomeDayButton_Click(object sender, RoutedEventArgs e)
		{
			_selectedDate = ShiftHomePeriod(_selectedDate, 1);
			RefreshAll();
		}

		private void OpenMarketingPhotoSourceButton_Click(object sender, RoutedEventArgs e)
		{
			if ((object)_selectedMarketingPhoto == null)
			{
				ShowStatus("Escolha uma foto da galeria para ver a origem e a licença.");
				return;
			}
			if (!Uri.TryCreate(string.IsNullOrWhiteSpace(_selectedMarketingPhoto.LandingUrl) ? _selectedMarketingPhoto.LicenseUrl : _selectedMarketingPhoto.LandingUrl, UriKind.Absolute, out Uri sourceUri))
			{
				ShowStatus("O endereço original desta foto não está disponível.");
				return;
			}
			Process.Start(new ProcessStartInfo(sourceUri.AbsoluteUri)
			{
				UseShellExecute = true
			});
			ShowStatus("Abrindo a página original para conferir autoria e licença.");
		}

		private void OpenStoryCreatorButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedMarketingSlots().Count == 0)
			{
				ShowStatus("Selecione pelo menos um horário para criar o story.");
				return;
			}
			StoryPreviewBusinessText.Text = BusinessDisplayName();
			StoryTitleTextBox.Text = MarketingStudioTitleValue();
			StorySubtitleTextBox.Text = MarketingStudioCopyValue();
			StoryPreviewPhoneText.Text = (string.IsNullOrWhiteSpace(_data.Settings.BusinessPhone) ? FormatPhone(_data.Settings.AccountPhone) : FormatPhone(_data.Settings.BusinessPhone));
			UpdateStoryPreview();
			StoryCreatorOverlay.Visibility = Visibility.Visible;
		}

		private void PreviousHomeDayButton_Click(object sender, RoutedEventArgs e)
		{
			_selectedDate = ShiftHomePeriod(_selectedDate, -1);
			RefreshAll();
		}

		private void PublishMarketingStudioButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Clipboard.SetImage(RenderMarketingStudioArtwork());
				List<string> slots = SelectedMarketingSlots();
				string caption = $"{MarketingStudioTitleValue()}\n\n{MarketingStudioCopyValue()}\n\nHorários: {string.Join(", ", slots)}";
				Process.Start(new ProcessStartInfo("https://wa.me/?text=" + Uri.EscapeDataString(caption))
				{
					UseShellExecute = true
				});
				ShowStatus("Arte copiada. No WhatsApp, cole com Ctrl+V para enviar ou publicar no Status.");
			}
			catch (Exception ex) when (((ex is InvalidOperationException || ex is Win32Exception) ? 1 : 0) != 0)
			{
				MessageBox.Show(this, "Não foi possível abrir o WhatsApp.\n\n" + ex.Message, "Marketing", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void RefreshMarketingStudio()
		{
			if (MarketingStudioSlot1Check != null)
			{
				CheckBox[] sourceChecks = new CheckBox[5] { MarketingSlot1Check, MarketingSlot2Check, MarketingSlot3Check, MarketingSlot4Check, MarketingSlot5Check };
				CheckBox[] studioChecks = new CheckBox[5] { MarketingStudioSlot1Check, MarketingStudioSlot2Check, MarketingStudioSlot3Check, MarketingStudioSlot4Check, MarketingStudioSlot5Check };
				for (int index = 0; index < studioChecks.Length; index++)
				{
					studioChecks[index].Visibility = sourceChecks[index].Visibility;
					studioChecks[index].Content = sourceChecks[index].Content;
					studioChecks[index].IsChecked = sourceChecks[index].Visibility == Visibility.Visible;
				}
				MarketingStudioBusinessIdentity();
				MarketingStudioConversationsText.Text = $"{_data.WhatsAppMessages.Count((WhatsAppMessage item) => item.CreatedAt.Date >= DateTime.Today.AddDays(-7.0) && item.Direction.Equals("entrada", StringComparison.OrdinalIgnoreCase))} novas";
				MarketingStudioReachText.Text = _data.Customers.Count.ToString(Brazil);
				MarketingStudioFreeSlotsText.Text = studioChecks.Count((CheckBox item) => item.Visibility == Visibility.Visible).ToString(Brazil);
				UpdateMarketingStudioPreview();
				InitializeMarketingEditor();
				MarketingEditorSyncSlotsFromLegacy();
			}
		}

		private void ReportFunnelStage_MouseEnter(object sender, MouseEventArgs e)
		{
			if (sender is Border { Tag: string stage })
			{
				UpdateReportsFunnelFocus(stage);
			}
		}

		private void ReportsOption3AppointmentsCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			DrawReportsOption3AppointmentsChart();
		}

		private void ReportsOption3OccupancyChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			DrawReportsOption3OccupancyChart();
		}

		private void ReportsPerformanceDonutCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			DrawReportsPerformanceDonut();
		}

		private void ReportsViewNoShowsButton_Click(object sender, RoutedEventArgs e)
		{
			DateTime periodStart = _selectedDate.Date.AddDays(-6.0);
			DateTime periodEnd = _selectedDate.Date.AddDays(1.0);
			Appointment noShow = (from item in _data.Appointments
				where item.Start >= periodStart && item.Start < periodEnd
				where item.Status == AppointmentStatus.NoShow
				orderby item.Start descending
				select item).FirstOrDefault();
			if (noShow != null)
			{
				_selectedDate = noShow.Start.Date;
				UpdateDateFilterButton();
			}
			ShowMainPage(MainPage.Agenda);
			RefreshAll(noShow?.Id);
			ShowStatus((noShow == null) ? "Agenda aberta para revisar confirmações e pagamentos." : $"Agenda aberta em {noShow.Start:dd/MM} para revisar a falta de {noShow.CustomerName}.");
		}

		private void ScheduleBoardScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (ScheduleBoardStickyHeaderGrid != null && ScheduleBoardGrid != null)
			{
				ScheduleBoardStickyHeaderGrid.Width = Math.Max(ScheduleBoardStickyHeaderGrid.MinWidth, ScheduleBoardGrid.ActualWidth);
				ScheduleBoardStickyHeaderGrid.RenderTransform = new TranslateTransform(0.0 - e.HorizontalOffset, 0.0);
				ScheduleBoardStickyHeaderGrid.Visibility = ((!(e.VerticalOffset > 0.5)) ? Visibility.Collapsed : Visibility.Visible);
			}
		}

		private async void SelectMarketingPhotoButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button { DataContext: MarketingPhotoSuggestion suggestion })
			{
				await ApplyMarketingPhotoAsync(suggestion);
			}
		}

		private void ShareAvailableSlotsWhatsAppButton_Click(object sender, RoutedEventArgs e)
		{
			List<string> slots = SelectedMarketingSlots();
			if (slots.Count == 0)
			{
				ShowStatus("Selecione pelo menos um horário para divulgar.");
				return;
			}
			string dayLabel = ((_marketingSlotsDate.Date == DateTime.Today) ? "hoje" : $"{_marketingSlotsDate:dddd, dd/MM}");
			string message = $"✨ Horários disponíveis {dayLabel} no {BusinessDisplayName()}: {string.Join(", ", slots)}. Quer reservar? Chame a gente por aqui!";
			Process.Start(new ProcessStartInfo("https://wa.me/?text=" + Uri.EscapeDataString(message))
			{
				UseShellExecute = true
			});
			ShowStatus("Horários preparados para compartilhar no WhatsApp.");
		}

		private void SnoozeAppointmentAssistantButton_Click(object sender, RoutedEventArgs e)
		{
			if ((object)_appointmentAssistantCurrent != null)
			{
				_appointmentAssistantSnoozedUntilByAppointment[_appointmentAssistantCurrent.Appointment.Id] = DateTime.Now.AddMinutes(10.0);
				_lastAppointmentAssistantAnnouncementKey = "";
				RefreshAppointmentAssistant();
				ShowStatus("Lembrete adiado por 10 minutos.");
			}
		}

		private void StoryEditor_Changed(object sender, RoutedEventArgs e)
		{
			UpdateStoryPreview();
		}

		private void StoryPaletteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			if (StoryPreviewCard != null)
			{
				(string, string, string) colors = ((StoryPaletteCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "orange") switch
				{
					"pink" => ("#D94679", "#8B1D4A", "#FFF0F5"), 
					"green" => ("#26745A", "#174B3B", "#EAF8F2"), 
					"dark" => ("#171614", "#000000", "#FFF4ED"), 
					_ => ("#F26A2E", "#C94513", "#FFF4ED"), 
				};
				StoryPreviewCard.Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(colors.Item1), (Color)ColorConverter.ConvertFromString(colors.Item2), new Point(0.0, 0.0), new Point(1.0, 1.0));
				StoryPreviewSlotsText.Foreground = Solid(colors.Item2);
				UpdateStoryPreview();
			}
		}

		private void TodayHomeButton_Click(object sender, RoutedEventArgs e)
		{
			_selectedDate = DateTime.Today;
			RefreshAll();
		}
}
