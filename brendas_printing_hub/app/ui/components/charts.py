"""
Chart widgets built on QtCharts. PySide6's QtCharts module ships with
PySide6-Addons (installed automatically with PySide6).
"""
from __future__ import annotations

from typing import Iterable, List, Sequence, Tuple

from PySide6.QtCharts import (
    QBarCategoryAxis,
    QBarSeries,
    QBarSet,
    QChart,
    QChartView,
    QLineSeries,
    QPieSeries,
    QValueAxis,
)
from PySide6.QtCore import Qt
from PySide6.QtGui import QColor, QPainter
from PySide6.QtWidgets import QWidget


PALETTE = [
    QColor("#2563EB"),  # blue
    QColor("#16A34A"),  # green
    QColor("#D97706"),  # amber
    QColor("#DC2626"),  # red
    QColor("#0EA5E9"),  # sky
    QColor("#7C3AED"),  # violet
    QColor("#0F766E"),  # teal
]


def _new_chart() -> QChart:
    chart = QChart()
    chart.legend().setVisible(True)
    chart.legend().setAlignment(Qt.AlignBottom)
    chart.setBackgroundRoundness(0)
    chart.setMargins(chart.margins())
    chart.setAnimationOptions(QChart.SeriesAnimations)
    chart.setBackgroundBrush(QColor("#FFFFFF"))
    return chart


def _wrap_view(chart: QChart) -> QChartView:
    view = QChartView(chart)
    view.setRenderHint(QPainter.Antialiasing)
    view.setMinimumHeight(260)
    view.setStyleSheet("background:#FFFFFF; border:none;")
    return view


def make_bar_chart(
    title: str,
    categories: Sequence[str],
    values: Sequence[float],
    *,
    series_label: str = "Sales",
) -> QChartView:
    """Vertical bar chart for category totals."""
    chart = _new_chart()
    chart.setTitle(title)

    bar_set = QBarSet(series_label)
    bar_set.setColor(PALETTE[0])
    bar_set.setBorderColor(PALETTE[0])
    for v in values:
        bar_set.append(float(v or 0))

    series = QBarSeries()
    series.append(bar_set)
    series.setLabelsVisible(False)
    chart.addSeries(series)

    axis_x = QBarCategoryAxis()
    axis_x.append(list(categories))
    chart.addAxis(axis_x, Qt.AlignBottom)
    series.attachAxis(axis_x)

    axis_y = QValueAxis()
    max_v = max(values) if values else 0
    axis_y.setRange(0, max(1, max_v * 1.15))
    axis_y.setLabelFormat("%d")
    chart.addAxis(axis_y, Qt.AlignLeft)
    series.attachAxis(axis_y)

    return _wrap_view(chart)


def make_line_chart(
    title: str,
    points: Sequence[Tuple[str, float]],
    *,
    series_label: str = "Revenue",
) -> QChartView:
    """Line chart for trend over a list of (label, value) points."""
    chart = _new_chart()
    chart.setTitle(title)

    series = QLineSeries()
    series.setName(series_label)
    series.setColor(PALETTE[0])
    pen = series.pen()
    pen.setWidth(3)
    series.setPen(pen)
    for i, (_, value) in enumerate(points):
        series.append(float(i), float(value or 0))
    chart.addSeries(series)

    axis_x = QBarCategoryAxis()
    axis_x.append([label for label, _ in points])
    chart.addAxis(axis_x, Qt.AlignBottom)
    series.attachAxis(axis_x)

    values = [v for _, v in points]
    max_v = max(values) if values else 0
    axis_y = QValueAxis()
    axis_y.setRange(0, max(1, max_v * 1.15))
    axis_y.setLabelFormat("%d")
    chart.addAxis(axis_y, Qt.AlignLeft)
    series.attachAxis(axis_y)

    return _wrap_view(chart)


def make_pie_chart(title: str, slices: Sequence[Tuple[str, float]]) -> QChartView:
    chart = _new_chart()
    chart.setTitle(title)

    series = QPieSeries()
    series.setHoleSize(0.45)  # donut

    total = sum(v for _, v in slices) or 1
    for i, (label, value) in enumerate(slices):
        s = series.append(label, float(value or 0))
        color = PALETTE[i % len(PALETTE)]
        s.setBrush(color)
        s.setBorderColor(QColor("#FFFFFF"))
        s.setBorderWidth(2)
        pct = (value / total) * 100 if total else 0
        s.setLabel(f"{label}  {pct:.0f}%")
        s.setLabelVisible(False)

    chart.addSeries(series)
    return _wrap_view(chart)


def make_grouped_bar_chart(
    title: str,
    categories: Sequence[str],
    series: Iterable[Tuple[str, Sequence[float]]],
) -> QChartView:
    """Grouped bars: each entry in `series` is (label, values_aligned_with_categories)."""
    chart = _new_chart()
    chart.setTitle(title)

    bar_series = QBarSeries()
    max_value = 0.0
    for i, (label, values) in enumerate(series):
        bar_set = QBarSet(label)
        bar_set.setColor(PALETTE[i % len(PALETTE)])
        bar_set.setBorderColor(PALETTE[i % len(PALETTE)])
        for v in values:
            v = float(v or 0)
            bar_set.append(v)
            if v > max_value:
                max_value = v
        bar_series.append(bar_set)

    chart.addSeries(bar_series)

    axis_x = QBarCategoryAxis()
    axis_x.append(list(categories))
    chart.addAxis(axis_x, Qt.AlignBottom)
    bar_series.attachAxis(axis_x)

    axis_y = QValueAxis()
    axis_y.setRange(0, max(1, max_value * 1.15))
    axis_y.setLabelFormat("%d")
    chart.addAxis(axis_y, Qt.AlignLeft)
    bar_series.attachAxis(axis_y)

    return _wrap_view(chart)
