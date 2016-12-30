﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Quandl.API;
using System.Threading.Tasks;

namespace Quandl.UI {
    public partial class QuandlViewer : Form {

        private QuandlService service;
        private string[] names = { "NASDAQ_MSFT", "NASDAQ_AAPL", "NASDAQ_GOOG" };
        private const int INTERVAL = 2000;

        public QuandlViewer() {
            InitializeComponent();
            service = new QuandlService();
        }

        private void displayButton_Click(object sender, EventArgs e) {
            //SequentialImplementation();
            ParallelImplementation();
        }

        private void ParallelImplementation() {
            var tasks = names.Select(n => Task.Run(() => RetrieveStockData(n)).ContinueWith((t) =>
            {
                var values = t.Result.GetValues();
                var seriesTask =
                    Task.Run(() => GetSeries(values, n));
                var trendTask = Task.Run(() => GetTrend(values, n));

                return new[] { seriesTask.Result, trendTask.Result };
            })).ToArray();
            Task.WaitAll(tasks);
            var seriesList = tasks.SelectMany(x => x.Result).ToList();
            DisplayData(seriesList);
            SaveImage("chart");

        }

        private void SequentialImplementation() {
            List<Series> seriesList = new List<Series>();

            foreach (var name in names) {
                StockData sd = RetrieveStockData(name);
                List<StockValue> values = sd.GetValues();
                seriesList.Add(GetSeries(values, name));
                seriesList.Add(GetTrend(values, name));
            }

            DisplayData(seriesList);
            SaveImage("chart");
        }

        private StockData RetrieveStockData(string name) {
            return service.GetData(name);
        }

        private Series GetSeries(List<StockValue> stockValues, string name) {
            Series series = new Series(name);
            series.ChartType = SeriesChartType.FastLine;

            int j = 0;
            for (int i = stockValues.Count - INTERVAL; i < stockValues.Count; i++) {
                series.Points.Add(new DataPoint(j++, stockValues[i].Close));
            }
            return series;
        }

        private Series GetTrend(List<StockValue> stockValues, string name) {
            double k, d;
            Series series = new Series(name + " Trend");
            series.ChartType = SeriesChartType.FastLine;

            var vals = stockValues.Select(x => x.Close).ToArray();
            LinearLeastSquaresFitting.Calculate(vals, out k, out d);

            int j = 0;
            for (int i = stockValues.Count - INTERVAL; i < stockValues.Count; i++) {
                series.Points.Add(new DataPoint(j++, k * i + d));
            }
            return series;
        }

        private void DisplayData(List<Series> seriesList) {
            chart.Series.Clear();
            foreach (Series series in seriesList) {
                chart.Series.Add(series);
            }
        }

        private void SaveImage(string fileName) {
            chart.SaveImage(fileName + ".jpg", ChartImageFormat.Jpeg);
        }
    }
}