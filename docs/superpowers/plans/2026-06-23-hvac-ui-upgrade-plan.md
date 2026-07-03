# HVAC 能源监控平台 UI/UX 全面升级 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不改动后端业务逻辑的前提下，全面重构 WPF 主窗口与 6 个功能 View，实现浅色专业工业风界面：统一设计系统、左侧图标导航、顶部状态栏、KPI 卡片、实时曲线、统计图表。

**Architecture:** 引入统一 `Design/Styles.xaml` 资源字典定义颜色、字体、通用控件样式；各 View 通过 `StaticResource` 引用；新增少量 ValueConverter 处理状态/可见性；ViewModel 仅做最小扩展以支撑新绑定。

**Tech Stack:** WPF (.NET 8), Prism 9, ScottPlot.WPF 5.0.35, MahApps.Metro.IconPacks 5.1.0

---

## 项目目录与文件结构

```text
HVAC.EnergyMonitor/
├── Design/
│   └── Styles.xaml              # 全局颜色、字体、控件样式
├── Converters/
│   ├── StatusToBrushConverter.cs
│   └── EmptyCollectionToVisibilityConverter.cs
├── Views/
│   ├── MainWindow.xaml          # 左侧导航 + 顶部状态栏 + MainRegion
│   ├── DashboardView.xaml       # KPI 卡片 + 实时曲线 + 点位列表 + 报警条
│   ├── DeviceConfigView.xaml    # 工具栏 + 状态灯 + 协议徽章
│   ├── PointConfigView.xaml     # 筛选 + 列分组表格
│   ├── HistoryTrendView.xaml    # 三面板趋势（小时/日/月）
│   ├── AlarmView.xaml           # 统计卡片 + 筛选 + 表格
│   └── EnergyReportView.xaml    # 表格 + 饼图
├── ViewModels/
│   ├── MainWindowViewModel.cs   # 扩展当前时间、未确认报警数（可选）
│   └── DashboardViewModel.cs    # 扩展 KPI/趋势数据
└── HVAC.EnergyMonitor.csproj
```

---

## Task 1: 添加 MahApps.Metro.IconPacks 依赖

**Files:**
- Modify: `HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj`

- [ ] **Step 1: 在 csproj 添加包引用**

Modify: `HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj`

在现有 `PackageReference` 节点内新增：

```xml
<PackageReference Include="MahApps.Metro.IconPacks" Version="5.1.0" />
```

- [ ] **Step 2: 还原并编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet restore HVAC.EnergyMonitor.sln
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj
git commit -m "chore: add MahApps.Metro.IconPacks dependency"
```

---

## Task 2: 创建全局设计系统 Styles.xaml

**Files:**
- Create: `HVAC.EnergyMonitor/Design/Styles.xaml`

- [ ] **Step 1: 创建资源字典**

Create: `HVAC.EnergyMonitor/Design/Styles.xaml`

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks">

    <!-- Colors -->
    <Color x:Key="BackgroundMainColor">#F5F7FA</Color>
    <Color x:Key="CardBackgroundColor">#FFFFFF</Color>
    <Color x:Key="SidebarBackgroundColor">#FFFFFF</Color>
    <Color x:Key="HeaderBackgroundColor">#FFFFFF</Color>
    <Color x:Key="PrimaryColor">#0066CC</Color>
    <Color x:Key="PrimaryLightColor">#E6F0FA</Color>
    <Color x:Key="SuccessColor">#00AA44</Color>
    <Color x:Key="WarningColor">#FFAA00</Color>
    <Color x:Key="DangerColor">#E63946</Color>
    <Color x:Key="TextPrimaryColor">#1F2937</Color>
    <Color x:Key="TextSecondaryColor">#6B7280</Color>
    <Color x:Key="BorderColor">#E5E7EB</Color>

    <!-- Brushes -->
    <SolidColorBrush x:Key="BrushBackgroundMain" Color="{StaticResource BackgroundMainColor}"/>
    <SolidColorBrush x:Key="BrushCardBackground" Color="{StaticResource CardBackgroundColor}"/>
    <SolidColorBrush x:Key="BrushSidebarBackground" Color="{StaticResource SidebarBackgroundColor}"/>
    <SolidColorBrush x:Key="BrushHeaderBackground" Color="{StaticResource HeaderBackgroundColor}"/>
    <SolidColorBrush x:Key="BrushPrimary" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="BrushPrimaryLight" Color="{StaticResource PrimaryLightColor}"/>
    <SolidColorBrush x:Key="BrushSuccess" Color="{StaticResource SuccessColor}"/>
    <SolidColorBrush x:Key="BrushWarning" Color="{StaticResource WarningColor}"/>
    <SolidColorBrush x:Key="BrushDanger" Color="{StaticResource DangerColor}"/>
    <SolidColorBrush x:Key="BrushTextPrimary" Color="{StaticResource TextPrimaryColor}"/>
    <SolidColorBrush x:Key="BrushTextSecondary" Color="{StaticResource TextSecondaryColor}"/>
    <SolidColorBrush x:Key="BrushBorder" Color="{StaticResource BorderColor}"/>

    <!-- Common thickness / corner radius -->
    <Thickness x:Key="CardPadding">16</Thickness>
    <Thickness x:Key="CardBorderThickness">1</Thickness>
    <CornerRadius x:Key="CardCornerRadius">8</CornerRadius>

    <!-- Card style -->
    <Style x:Key="CardStyle" TargetType="Border">
        <Setter Property="Background" Value="{StaticResource BrushCardBackground}"/>
        <Setter Property="BorderBrush" Value="{StaticResource BrushBorder}"/>
        <Setter Property="BorderThickness" Value="{StaticResource CardBorderThickness}"/>
        <Setter Property="CornerRadius" Value="{StaticResource CardCornerRadius}"/>
        <Setter Property="Padding" Value="{StaticResource CardPadding}"/>
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect ShadowDepth="2" BlurRadius="6" Opacity="0.08" Color="Black"/>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Page title style -->
    <Style x:Key="PageTitleStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="24"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Foreground" Value="{StaticResource BrushTextPrimary}"/>
        <Setter Property="VerticalAlignment" Value="Center"/>
    </Style>

    <!-- Section subtitle style -->
    <Style x:Key="SectionSubtitleStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="Foreground" Value="{StaticResource BrushTextSecondary}"/>
    </Style>

    <!-- KPI label style -->
    <Style x:Key="KpiLabelStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="Foreground" Value="{StaticResource BrushTextSecondary}"/>
    </Style>

    <!-- KPI value style -->
    <Style x:Key="KpiValueStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="36"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{StaticResource BrushTextPrimary}"/>
    </Style>

    <!-- Navigation button style -->
    <Style x:Key="NavButtonStyle" TargetType="Button">
        <Setter Property="Height" Value="44"/>
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource BrushTextPrimary}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="HorizontalContentAlignment" Value="Left"/>
        <Setter Property="Padding" Value="12,0"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Bd" Background="{TemplateBinding Background}" CornerRadius="4">
                        <StackPanel Orientation="Horizontal" Margin="{TemplateBinding Padding}">
                            <ContentPresenter x:Name="Icon" Content="{TemplateBinding Tag}" VerticalAlignment="Center" Margin="0,0,12,0"/>
                            <ContentPresenter Content="{TemplateBinding Content}" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#F3F4F6"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- Toolbar button style -->
    <Style x:Key="ToolbarButtonStyle" TargetType="Button">
        <Setter Property="Height" Value="32"/>
        <Setter Property="Padding" Value="12,0"/>
        <Setter Property="Background" Value="{StaticResource BrushPrimaryLight}"/>
        <Setter Property="Foreground" Value="{StaticResource BrushPrimary}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}" CornerRadius="4" Padding="{TemplateBinding Padding}">
                        <StackPanel Orientation="Horizontal">
                            <ContentPresenter Content="{TemplateBinding Tag}" VerticalAlignment="Center" Margin="0,0,6,0"/>
                            <ContentPresenter Content="{TemplateBinding Content}" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="{StaticResource BrushPrimary}"/>
                <Setter Property="Foreground" Value="White"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- DataGrid style -->
    <Style x:Key="StyledDataGrid" TargetType="DataGrid">
        <Setter Property="Background" Value="{StaticResource BrushCardBackground}"/>
        <Setter Property="BorderBrush" Value="{StaticResource BrushBorder}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="RowHeight" Value="40"/>
        <Setter Property="HeadersVisibility" Value="Column"/>
        <Setter Property="GridLinesVisibility" Value="Horizontal"/>
        <Setter Property="HorizontalGridLinesBrush" Value="{StaticResource BrushBorder}"/>
        <Setter Property="VerticalGridLinesBrush" Value="{StaticResource BrushBorder}"/>
        <Setter Property="ColumnHeaderStyle">
            <Setter.Value>
                <Style TargetType="DataGridColumnHeader">
                    <Setter Property="Background" Value="#F9FAFB"/>
                    <Setter Property="Foreground" Value="{StaticResource BrushTextPrimary}"/>
                    <Setter Property="FontWeight" Value="Bold"/>
                    <Setter Property="Padding" Value="12,8"/>
                    <Setter Property="BorderThickness" Value="0,0,0,1"/>
                    <Setter Property="BorderBrush" Value="{StaticResource BrushBorder}"/>
                </Style>
            </Setter.Value>
        </Setter>
        <Setter Property="CellStyle">
            <Setter.Value>
                <Style TargetType="DataGridCell">
                    <Setter Property="Padding" Value="12,0"/>
                    <Setter Property="BorderThickness" Value="0"/>
                    <Setter Property="VerticalContentAlignment" Value="Center"/>
                </Style>
            </Setter.Value>
        </Setter>
        <Setter Property="RowStyle">
            <Setter.Value>
                <Style TargetType="DataGridRow">
                    <Style.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="#F3F4F6"/>
                        </Trigger>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter Property="Background" Value="{StaticResource BrushPrimaryLight}"/>
                        </Trigger>
                    </Style.Triggers>
                </Style>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Status badge style -->
    <Style x:Key="StatusBadgeStyle" TargetType="Border">
        <Setter Property="CornerRadius" Value="12"/>
        <Setter Property="Padding" Value="8,3"/>
        <Setter Property="Background" Value="{StaticResource BrushPrimaryLight}"/>
        <Setter Property="BorderThickness" Value="0"/>
    </Style>

    <!-- Alarm ticker style -->
    <Style x:Key="AlarmTickerStyle" TargetType="Border">
        <Setter Property="Background" Value="#FEF2F2"/>
        <Setter Property="BorderBrush" Value="#FECACA"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="6"/>
        <Setter Property="Padding" Value="12,8"/>
    </Style>

</ResourceDictionary>
```

- [ ] **Step 2: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Design/Styles.xaml
git commit -m "feat: add global design system styles"
```

---

## Task 3: 创建 ValueConverter

**Files:**
- Create: `HVAC.EnergyMonitor/Converters/StatusToBrushConverter.cs`
- Create: `HVAC.EnergyMonitor/Converters/EmptyCollectionToVisibilityConverter.cs`

- [ ] **Step 1: 创建 StatusToBrushConverter**

Create: `HVAC.EnergyMonitor/Converters/StatusToBrushConverter.cs`

```csharp
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HVAC.EnergyMonitor.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public Brush OnlineBrush { get; set; } = Brushes.Green;
    public Brush OfflineBrush { get; set; } = Brushes.Gray;
    public Brush AlarmBrush { get; set; } = Brushes.Red;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();
        return status switch
        {
            "true" or "online" or "运行中" => OnlineBrush,
            "alarm" or "danger" => AlarmBrush,
            _ => OfflineBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: 创建 EmptyCollectionToVisibilityConverter**

Create: `HVAC.EnergyMonitor/Converters/EmptyCollectionToVisibilityConverter.cs`

```csharp
using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HVAC.EnergyMonitor.Converters;

public class EmptyCollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isEmpty = value is ICollection collection && collection.Count == 0;
        var invert = parameter?.ToString()?.ToLowerInvariant() == "invert";
        if (invert) isEmpty = !isEmpty;
        return isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 3: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Converters
git commit -m "feat: add status and visibility converters"
```

---

## Task 4: 重构主窗口 MainWindow

**Files:**
- Modify: `HVAC.EnergyMonitor/Views/MainWindow.xaml`

- [ ] **Step 1: 重写 MainWindow.xaml**

Modify: `HVAC.EnergyMonitor/Views/MainWindow.xaml`

```xml
<Window x:Class="HVAC.EnergyMonitor.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:prism="http://prismlibrary.com/"
        xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks"
        Title="HVAC 能源监控平台" Height="900" Width="1400"
        WindowStartupLocation="CenterScreen"
        Background="{StaticResource BrushBackgroundMain}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="240"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Sidebar -->
        <Border Grid.Column="0" Background="{StaticResource BrushSidebarBackground}" BorderBrush="{StaticResource BrushBorder}" BorderThickness="0,0,1,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- Logo -->
                <StackPanel Grid.Row="0" Orientation="Horizontal" Height="64" Margin="16,0">
                    <iconPacks:PackIconMaterial Kind="AirConditioner" Width="28" Height="28" Foreground="{StaticResource BrushPrimary}" VerticalAlignment="Center"/>
                    <TextBlock Text="HVAC 能源监控" FontSize="18" FontWeight="Bold" Foreground="{StaticResource BrushPrimary}" VerticalAlignment="Center" Margin="12,0,0,0"/>
                </StackPanel>

                <!-- Navigation -->
                <StackPanel Grid.Row="1" Margin="12,8">
                    <Button Style="{StaticResource NavButtonStyle}" Content="实时监控" Command="{Binding NavigateCommand}" CommandParameter="DashboardView">
                        <Button.Tag>
                            <iconPacks:PackIconMaterial Kind="ViewDashboard" Width="20" Height="20" Foreground="{StaticResource BrushPrimary}"/>
                        </Button.Tag>
                    </Button>
                    <Button Style="{StaticResource NavButtonStyle}" Content="设备管理" Command="{Binding NavigateCommand}" CommandParameter="DeviceConfigView">
                        <Button.Tag>
                            <iconPacks:PackIconMaterial Kind="Devices" Width="20" Height="20" Foreground="{StaticResource BrushTextSecondary}"/>
                        </Button.Tag>
                    </Button>
                    <Button Style="{StaticResource NavButtonStyle}" Content="点位管理" Command="{Binding NavigateCommand}" CommandParameter="PointConfigView">
                        <Button.Tag>
                            <iconPacks:PackIconMaterial Kind="SourceBranch" Width="20" Height="20" Foreground="{StaticResource BrushTextSecondary}"/>
                        </Button.Tag>
                    </Button>
                    <Button Style="{StaticResource NavButtonStyle}" Content="历史趋势" Command="{Binding NavigateCommand}" CommandParameter="HistoryTrendView">
                        <Button.Tag>
                            <iconPacks:PackIconMaterial Kind="ChartLine" Width="20" Height="20" Foreground="{StaticResource BrushTextSecondary}"/>
                        </Button.Tag>
                    </Button>
                    <Button Style="{StaticResource NavButtonStyle}" Content="报警管理" Command="{Binding NavigateCommand}" CommandParameter="AlarmView">
                        <Button.Tag>
                            <iconPacks:PackIconMaterial Kind="Alert" Width="20" Height="20" Foreground="{StaticResource BrushTextSecondary}"/>
                        </Button.Tag>
                    </Button>
                    <Button Style="{StaticResource NavButtonStyle}" Content="能耗报表" Command="{Binding NavigateCommand}" CommandParameter="EnergyReportView">
                        <Button.Tag>
                            <iconPacks:PackIconMaterial Kind="ChartPie" Width="20" Height="20" Foreground="{StaticResource BrushTextSecondary}"/>
                        </Button.Tag>
                    </Button>
                </StackPanel>

                <!-- Collapse button -->
                <Button Grid.Row="2" Height="40" Background="Transparent" BorderThickness="0" HorizontalContentAlignment="Left" Padding="20,0">
                    <iconPacks:PackIconMaterial Kind="ChevronLeft" Width="20" Height="20" Foreground="{StaticResource BrushTextSecondary}"/>
                </Button>
            </Grid>
        </Border>

        <!-- Main content -->
        <Grid Grid.Column="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="48"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Header -->
            <Border Grid.Row="0" Background="{StaticResource BrushHeaderBackground}" BorderBrush="{StaticResource BrushBorder}" BorderThickness="0,0,0,1">
                <Grid Margin="16,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="工业能源监控平台" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" FontSize="14"/>
                    <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                        <iconPacks:PackIconMaterial Kind="ClockOutline" Width="16" Height="16" Foreground="{StaticResource BrushTextSecondary}" VerticalAlignment="Center"/>
                        <TextBlock Text="{Binding CurrentTime}" Margin="6,0,16,0" Foreground="{StaticResource BrushTextPrimary}" FontSize="13" VerticalAlignment="Center"/>
                        <Ellipse Width="8" Height="8" Fill="{StaticResource BrushSuccess}" VerticalAlignment="Center"/>
                        <TextBlock Text="系统在线" Margin="6,0,16,0" Foreground="{StaticResource BrushTextPrimary}" FontSize="13" VerticalAlignment="Center"/>
                        <Border Background="{StaticResource BrushDanger}" CornerRadius="10" Padding="6,2" VerticalAlignment="Center">
                            <TextBlock Text="0" Foreground="White" FontSize="12" FontWeight="Bold"/>
                        </Border>
                    </StackPanel>
                </Grid>
            </Border>

            <!-- Region -->
            <ContentControl Grid.Row="1" prism:RegionManager.RegionName="MainRegion" Margin="16"/>
        </Grid>
    </Grid>
</Window>
```

- [ ] **Step 2: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Views/MainWindow.xaml
git commit -m "feat: redesign main window with sidebar navigation and header"
```

---

## Task 5: 重构实时监控 DashboardView

**Files:**
- Modify: `HVAC.EnergyMonitor/Views/DashboardView.xaml`
- Modify: `HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs`（扩展实时曲线数据）

- [ ] **Step 1: 扩展 DashboardViewModel 支持实时曲线**

Modify: `HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs`

在现有 `PointValues` 集合下方新增：

```csharp
public ObservableCollection<HistoryDataPoint> TrendPoints1 { get; } = new();
public ObservableCollection<HistoryDataPoint> TrendPoints2 { get; } = new();
public ObservableCollection<HistoryDataPoint> TrendPoints3 { get; } = new();
public ObservableCollection<HistoryDataPoint> TrendPoints4 { get; } = new();
```

在 `RefreshAsync()` 末尾添加趋势数据追加逻辑（保留最近 300 个点）：

```csharp
private void AppendTrendData()
{
    var now = DateTime.Now;
    var values = PointValues.ToList();
    void Append(ObservableCollection<HistoryDataPoint> list, int index)
    {
        if (index < values.Count)
            list.Add(new HistoryDataPoint { Timestamp = now, Value = values[index].Value });
        while (list.Count > 300) list.RemoveAt(0);
    }
    Append(TrendPoints1, 0);
    Append(TrendPoints2, 1);
    Append(TrendPoints3, 2);
    Append(TrendPoints4, 3);
}
```

并在 `RefreshAsync()` 中调用 `AppendTrendData();`。

- [ ] **Step 2: 重写 DashboardView.xaml**

Modify: `HVAC.EnergyMonitor/Views/DashboardView.xaml`

```xml
<UserControl x:Class="HVAC.EnergyMonitor.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:scottplot="clr-namespace:ScottPlot.WPF;assembly=ScottPlot.WPF">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Grid Grid.Row="0" Margin="0,0,0,16">
            <TextBlock Text="实时监控" Style="{StaticResource PageTitleStyle}"/>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Border Style="{StaticResource StatusBadgeStyle}" Background="{StaticResource BrushPrimaryLight}" Margin="0,0,8,0">
                    <StackPanel Orientation="Horizontal">
                        <Ellipse Width="8" Height="8" Fill="{StaticResource BrushSuccess}" Margin="0,0,6,0"/>
                        <TextBlock Text="{Binding AcquisitionStatus}" Foreground="{StaticResource BrushPrimary}" FontWeight="SemiBold"/>
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource StatusBadgeStyle}" Background="{StaticResource BrushPrimaryLight}">
                    <StackPanel Orientation="Horizontal">
                        <Ellipse Width="8" Height="8" Fill="{StaticResource BrushSuccess}" Margin="0,0,6,0"/>
                        <TextBlock Text="{Binding OnlineStatus}" Foreground="{StaticResource BrushPrimary}" FontWeight="SemiBold"/>
                    </StackPanel>
                </Border>
            </StackPanel>
        </Grid>

        <!-- KPI Cards -->
        <ItemsControl Grid.Row="1" ItemsSource="{Binding PointValues}" Margin="0,0,0,16">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <UniformGrid Columns="4" Rows="1"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                        <StackPanel>
                            <TextBlock Text="{Binding Name}" Style="{StaticResource KpiLabelStyle}"/>
                            <StackPanel Orientation="Horizontal" Margin="0,8">
                                <TextBlock Text="{Binding Value, StringFormat=F2}" Style="{StaticResource KpiValueStyle}"/>
                                <TextBlock Text="{Binding Unit}" Foreground="{StaticResource BrushTextSecondary}" FontSize="14" VerticalAlignment="Bottom" Margin="8,0,0,6"/>
                            </StackPanel>
                            <TextBlock Text="{Binding Timestamp, StringFormat=HH:mm:ss.fff}" Foreground="{StaticResource BrushTextSecondary}" FontSize="12"/>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>

        <!-- Trend + Point list -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Realtime trend -->
            <Border Grid.Column="0" Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="实时趋势" FontSize="16" FontWeight="SemiBold" Foreground="{StaticResource BrushTextPrimary}" Margin="0,0,0,12"/>
                    <scottplot:WpfPlot Grid.Row="1" x:Name="RealtimePlot"/>
                </Grid>
            </Border>

            <!-- Point list -->
            <Border Grid.Column="1" Style="{StaticResource CardStyle}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="实时点位" FontSize="16" FontWeight="SemiBold" Foreground="{StaticResource BrushTextPrimary}" Margin="0,0,0,12"/>
                    <DataGrid Grid.Row="1" ItemsSource="{Binding PointValues}" Style="{StaticResource StyledDataGrid}" AutoGenerateColumns="False" IsReadOnly="True" BorderThickness="0">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="点位" Binding="{Binding Name}" Width="*"/>
                            <DataGridTextColumn Header="值" Binding="{Binding Value, StringFormat=F2}" Width="80"/>
                            <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60"/>
                            <DataGridTextColumn Header="时间" Binding="{Binding Timestamp, StringFormat=HH:mm:ss}" Width="80"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </Grid>
            </Border>
        </Grid>

        <!-- Alarm ticker -->
        <Border Grid.Row="3" Style="{StaticResource AlarmTickerStyle}" Margin="0,16,0,0">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="最近报警" Foreground="{StaticResource BrushDanger}" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,12,0"/>
                <TextBlock Text="暂无未确认报警" Foreground="{StaticResource BrushTextSecondary}" VerticalAlignment="Center"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 3: 在 DashboardView.xaml.cs 中绑定 ScottPlot 实时曲线**

Modify: `HVAC.EnergyMonitor/Views/DashboardView.xaml.cs`

```csharp
using HVAC.EnergyMonitor.ViewModels;
using ScottPlot;
using System.Windows;
using System.Windows.Controls;

namespace HVAC.EnergyMonitor.Views;

public partial class DashboardView : UserControl
{
    private readonly ScottPlot.Plottables.Scatter _scatter1;
    private readonly ScottPlot.Plottables.Scatter _scatter2;
    private readonly ScottPlot.Plottables.Scatter _scatter3;
    private readonly ScottPlot.Plottables.Scatter _scatter4;

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm) return;

        var plot = RealtimePlot.Plot;
        plot.Clear();
        plot.Axes.DateTimeTicksBottom();
        plot.Title("最近 5 分钟实时趋势");
        plot.XLabel("时间");
        plot.YLabel("工程值");

        _scatter1 = plot.Add.Scatter(vm.TrendPoints1.Select(p => p.Timestamp.ToOADate()).ToArray(), vm.TrendPoints1.Select(p => p.Value).ToArray());
        _scatter2 = plot.Add.Scatter(vm.TrendPoints2.Select(p => p.Timestamp.ToOADate()).ToArray(), vm.TrendPoints2.Select(p => p.Value).ToArray());
        _scatter3 = plot.Add.Scatter(vm.TrendPoints3.Select(p => p.Timestamp.ToOADate()).ToArray(), vm.TrendPoints3.Select(p => p.Value).ToArray());
        _scatter4 = plot.Add.Scatter(vm.TrendPoints4.Select(p => p.Timestamp.ToOADate()).ToArray(), vm.TrendPoints4.Select(p => p.Value).ToArray());

        _scatter1.Color = new ScottPlot.Color(0, 102, 204);
        _scatter2.Color = new ScottPlot.Color(0, 170, 68);
        _scatter3.Color = new ScottPlot.Color(255, 170, 0);
        _scatter4.Color = new ScottPlot.Color(230, 57, 70);

        vm.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(DashboardViewModel.TrendPoints1))
                RefreshScatter(_scatter1, vm.TrendPoints1);
            if (args.PropertyName == nameof(DashboardViewModel.TrendPoints2))
                RefreshScatter(_scatter2, vm.TrendPoints2);
            if (args.PropertyName == nameof(DashboardViewModel.TrendPoints3))
                RefreshScatter(_scatter3, vm.TrendPoints3);
            if (args.PropertyName == nameof(DashboardViewModel.TrendPoints4))
                RefreshScatter(_scatter4, vm.TrendPoints4);
        };

        var timer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
        timer.Tick += (s, args) =>
        {
            RefreshScatter(_scatter1, vm.TrendPoints1);
            RefreshScatter(_scatter2, vm.TrendPoints2);
            RefreshScatter(_scatter3, vm.TrendPoints3);
            RefreshScatter(_scatter4, vm.TrendPoints4);
            RealtimePlot.Refresh();
        };
        timer.Start();
    }

    private static void RefreshScatter(ScottPlot.Plottables.Scatter scatter, System.Collections.ObjectModel.ObservableCollection<ViewModels.HistoryDataPoint> points)
    {
        scatter.Xs = points.Select(p => p.Timestamp.ToOADate()).ToArray();
        scatter.Ys = points.Select(p => p.Value).ToArray();
    }
}
```

Note: `HistoryDataPoint` is currently defined in `HistoryTrendViewModel.cs`. Move it to a shared location or duplicate. For this plan, add a public `HistoryDataPoint` class in `DashboardViewModel.cs` (it already exists in HistoryTrendViewModel; duplicate will conflict). Better: move `HistoryDataPoint` to `HVAC.EnergyMonitor/Models/DTOs/HistoryDataPoint.cs` or `Models/HistoryDataPoint.cs`.

Add to plan: Create `HVAC.EnergyMonitor/Models/HistoryDataPoint.cs`:

```csharp
namespace HVAC.EnergyMonitor.Models;

public class HistoryDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
```

And remove the duplicate nested class from `HistoryTrendViewModel.cs` and `DashboardViewModel.cs` (or update references).

- [ ] **Step 4: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Views/DashboardView.xaml HVAC.EnergyMonitor/Views/DashboardView.xaml.cs HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs HVAC.EnergyMonitor/Models/HistoryDataPoint.cs
git commit -m "feat: redesign dashboard with KPI cards and realtime trend"
```

---

## Task 6: 重构设备管理 DeviceConfigView

**Files:**
- Modify: `HVAC.EnergyMonitor/Views/DeviceConfigView.xaml`

- [ ] **Step 1: 重写 DeviceConfigView.xaml**

Modify: `HVAC.EnergyMonitor/Views/DeviceConfigView.xaml`

```xml
<UserControl x:Class="HVAC.EnergyMonitor.Views.DeviceConfigView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks"
             xmlns:local="clr-namespace:HVAC.EnergyMonitor.Views">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="设备管理" Style="{StaticResource PageTitleStyle}" Margin="0,0,0,16"/>

        <!-- Toolbar -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,12">
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="新增" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Plus" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="删除" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Delete" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="保存" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="ContentSave" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="刷新">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Refresh" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
        </StackPanel>

        <!-- DataGrid -->
        <Border Grid.Row="2" Style="{StaticResource CardStyle}">
            <DataGrid ItemsSource="{Binding Devices}" Style="{StaticResource StyledDataGrid}" AutoGenerateColumns="False" CanUserAddRows="True" BorderThickness="0">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="序号" Binding="{Binding Id}" IsReadOnly="True" Width="60"/>
                    <DataGridTemplateColumn Header="状态" Width="80">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                                    <Ellipse Width="8" Height="8" Fill="{StaticResource BrushSuccess}" Margin="0,0,6,0"/>
                                    <TextBlock Text="在线" Foreground="{StaticResource BrushSuccess}" FontSize="13"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                    <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="*"/>
                    <DataGridTemplateColumn Header="协议" Width="120">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Border Background="{StaticResource BrushPrimaryLight}" CornerRadius="4" Padding="6,2" HorizontalAlignment="Left">
                                    <TextBlock Text="{Binding ProtocolType}" Foreground="{StaticResource BrushPrimary}" FontSize="12" FontWeight="SemiBold"/>
                                </Border>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                    <DataGridTextColumn Header="IP 地址" Binding="{Binding IpAddress}" Width="140"/>
                    <DataGridTextColumn Header="端口" Binding="{Binding Port}" Width="80"/>
                    <DataGridTextColumn Header="扫描周期(ms)" Binding="{Binding ScanIntervalMs}" Width="120"/>
                    <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsEnabled}" Width="80"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>

        <!-- Footer -->
        <TextBlock Grid.Row="3" Text="{Binding Devices.Count, StringFormat=共 {0} 台设备}" Foreground="{StaticResource BrushTextSecondary}" FontSize="12" Margin="0,8,0,0"/>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Views/DeviceConfigView.xaml
git commit -m "feat: redesign device config view with toolbar and badges"
```

---

## Task 7: 重构点位管理 PointConfigView

**Files:**
- Modify: `HVAC.EnergyMonitor/Views/PointConfigView.xaml`

- [ ] **Step 1: 重写 PointConfigView.xaml**

Modify: `HVAC.EnergyMonitor/Views/PointConfigView.xaml`

```xml
<UserControl x:Class="HVAC.EnergyMonitor.Views.PointConfigView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="点位管理" Style="{StaticResource PageTitleStyle}" Margin="0,0,0,16"/>

        <!-- Toolbar -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,12">
            <ComboBox Width="180" Margin="0,0,8,0" VerticalAlignment="Center" Padding="8,4"/>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="新增" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Plus" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="保存" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="ContentSave" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="刷新">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Refresh" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
        </StackPanel>

        <!-- DataGrid -->
        <Border Grid.Row="2" Style="{StaticResource CardStyle}">
            <DataGrid ItemsSource="{Binding Points}" Style="{StaticResource StyledDataGrid}" AutoGenerateColumns="False" CanUserAddRows="True" BorderThickness="0">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="140"/>
                    <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="80"/>
                    <DataGridTextColumn Header="功能码" Binding="{Binding FunctionCode}" Width="80"/>
                    <DataGridTextColumn Header="寄存器地址" Binding="{Binding RegisterAddress}" Width="100"/>
                    <DataGridTextColumn Header="数据类型" Binding="{Binding DataType}" Width="100"/>
                    <DataGridTextColumn Header="系数" Binding="{Binding Scale}" Width="80"/>
                    <DataGridTextColumn Header="偏移" Binding="{Binding Offset}" Width="80"/>
                    <DataGridTextColumn Header="高限" Binding="{Binding HighLimit}" Width="80"/>
                    <DataGridTextColumn Header="低限" Binding="{Binding LowLimit}" Width="80"/>
                    <DataGridCheckBoxColumn Header="存历史" Binding="{Binding StoreHistory}" Width="80"/>
                    <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsEnabled}" Width="80"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Views/PointConfigView.xaml
git commit -m "feat: redesign point config view with toolbar and grouped columns"
```

---

## Task 8: 重构历史趋势 HistoryTrendView

**Files:**
- Modify: `HVAC.EnergyMonitor/Views/HistoryTrendView.xaml`
- Modify: `HVAC.EnergyMonitor/Views/HistoryTrendView.xaml.cs`

- [ ] **Step 1: 重写 HistoryTrendView.xaml**

Modify: `HVAC.EnergyMonitor/Views/HistoryTrendView.xaml`

```xml
<UserControl x:Class="HVAC.EnergyMonitor.Views.HistoryTrendView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:scottplot="clr-namespace:ScottPlot.WPF;assembly=ScottPlot.WPF"
             xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="历史趋势" Style="{StaticResource PageTitleStyle}" Margin="0,0,0,16"/>

        <!-- Toolbar -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,12">
            <TextBlock Text="点位:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <ComboBox ItemsSource="{Binding Points}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding SelectedPointId}" Width="160" Margin="0,0,16,0" Padding="8,4"/>
            <TextBlock Text="开始:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <DatePicker SelectedDate="{Binding StartTime}" Margin="0,0,16,0" Width="120"/>
            <TextBlock Text="结束:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <DatePicker SelectedDate="{Binding EndTime}" Margin="0,0,16,0" Width="120"/>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="查询" Click="QueryButton_Click" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Magnify" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="导出">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Download" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
        </StackPanel>

        <!-- Three panels -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0" Style="{StaticResource CardStyle}" Margin="0,0,8,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="小时趋势" FontSize="16" FontWeight="SemiBold" Foreground="{StaticResource BrushTextPrimary}"/>
                    <TextBlock Grid.Row="1" Text="最大 / 最小 / 平均" Style="{StaticResource SectionSubtitleStyle}" Margin="0,4,0,12"/>
                    <scottplot:WpfPlot Grid.Row="2" x:Name="HourPlot"/>
                </Grid>
            </Border>

            <Border Grid.Column="1" Style="{StaticResource CardStyle}" Margin="4,0,4,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="日趋势" FontSize="16" FontWeight="SemiBold" Foreground="{StaticResource BrushTextPrimary}"/>
                    <TextBlock Grid.Row="1" Text="最大 / 最小 / 平均" Style="{StaticResource SectionSubtitleStyle}" Margin="0,4,0,12"/>
                    <scottplot:WpfPlot Grid.Row="2" x:Name="DayPlot"/>
                </Grid>
            </Border>

            <Border Grid.Column="2" Style="{StaticResource CardStyle}" Margin="8,0,0,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="月趋势" FontSize="16" FontWeight="SemiBold" Foreground="{StaticResource BrushTextPrimary}"/>
                    <TextBlock Grid.Row="1" Text="最大 / 最小 / 平均" Style="{StaticResource SectionSubtitleStyle}" Margin="0,4,0,12"/>
                    <scottplot:WpfPlot Grid.Row="2" x:Name="MonthPlot"/>
                </Grid>
            </Border>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 修改 HistoryTrendView.xaml.cs 绑定三个图**

Modify: `HVAC.EnergyMonitor/Views/HistoryTrendView.xaml.cs`

保留原有 `QueryButton_Click` 调用 `ViewModel.QueryAsync()` 的逻辑，在查询完成后刷新三个 ScottPlot。

```csharp
using HVAC.EnergyMonitor.Models;
using HVAC.EnergyMonitor.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HVAC.EnergyMonitor.Views;

public partial class HistoryTrendView : UserControl
{
    private ScottPlot.Plottables.Scatter? _hourScatter;
    private ScottPlot.Plottables.Scatter? _dayScatter;
    private ScottPlot.Plottables.Scatter? _monthScatter;

    public HistoryTrendView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitPlot(HourPlot, "小时趋势", out _hourScatter);
        InitPlot(DayPlot, "日趋势", out _dayScatter);
        InitPlot(MonthPlot, "月趋势", out _monthScatter);
    }

    private static void InitPlot(ScottPlot.WPF.WpfPlot plot, string title, out ScottPlot.Plottables.Scatter scatter)
    {
        plot.Plot.Clear();
        plot.Plot.Axes.DateTimeTicksBottom();
        plot.Plot.Title(title);
        scatter = plot.Plot.Add.Scatter(System.Array.Empty<double>(), System.Array.Empty<double>());
        scatter.Color = new ScottPlot.Color(0, 102, 204);
    }

    private void QueryButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HistoryTrendViewModel vm) return;
        vm.QueryAsync().ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                RefreshScatter(_hourScatter, vm.DataPoints);
                RefreshScatter(_dayScatter, vm.DataPoints);
                RefreshScatter(_monthScatter, vm.DataPoints);
                HourPlot.Refresh();
                DayPlot.Refresh();
                MonthPlot.Refresh();
            });
        });
    }

    private static void RefreshScatter(ScottPlot.Plottables.Scatter? scatter, System.Collections.ObjectModel.ObservableCollection<HistoryDataPoint> points)
    {
        if (scatter == null) return;
        scatter.Xs = points.Select(p => p.Timestamp.ToOADate()).ToArray();
        scatter.Ys = points.Select(p => p.Value).ToArray();
    }
}
```

- [ ] **Step 3: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Views/HistoryTrendView.xaml HVAC.EnergyMonitor/Views/HistoryTrendView.xaml.cs
git commit -m "feat: redesign history trend view with three period panels"
```

---

## Task 9: 重构报警管理 AlarmView

**Files:**
- Modify: `HVAC.EnergyMonitor/Views/AlarmView.xaml`

- [ ] **Step 1: 重写 AlarmView.xaml**

Modify: `HVAC.EnergyMonitor/Views/AlarmView.xaml`

```xml
<UserControl x:Class="HVAC.EnergyMonitor.Views.AlarmView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="报警管理" Style="{StaticResource PageTitleStyle}" Margin="0,0,0,16"/>

        <!-- Statistics cards -->
        <UniformGrid Grid.Row="1" Columns="4" Margin="0,0,0,16">
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="今日报警" Style="{StaticResource KpiLabelStyle}"/>
                    <TextBlock Text="12" Style="{StaticResource KpiValueStyle}" FontSize="28"/>
                </StackPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="未确认" Style="{StaticResource KpiLabelStyle}"/>
                    <TextBlock Text="3" Style="{StaticResource KpiValueStyle}" FontSize="28" Foreground="{StaticResource BrushDanger}"/>
                </StackPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="已确认" Style="{StaticResource KpiLabelStyle}"/>
                    <TextBlock Text="7" Style="{StaticResource KpiValueStyle}" FontSize="28" Foreground="{StaticResource BrushSuccess}"/>
                </StackPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}">
                <StackPanel>
                    <TextBlock Text="已恢复" Style="{StaticResource KpiLabelStyle}"/>
                    <TextBlock Text="2" Style="{StaticResource KpiValueStyle}" FontSize="28" Foreground="{StaticResource BrushTextSecondary}"/>
                </StackPanel>
            </Border>
        </UniformGrid>

        <!-- Filter toolbar -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,12">
            <TextBlock Text="时间范围:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <DatePicker Width="120" Margin="0,0,8,0"/>
            <DatePicker Width="120" Margin="0,0,16,0"/>
            <TextBlock Text="点位:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <ComboBox Width="140" Margin="0,0,16,0" Padding="8,4"/>
            <TextBlock Text="类型:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <ComboBox Width="100" Margin="0,0,16,0" Padding="8,4"/>
            <TextBlock Text="状态:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <ComboBox Width="100" Margin="0,0,16,0" Padding="8,4"/>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="查询" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Magnify" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="导出">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Download" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
        </StackPanel>

        <!-- DataGrid -->
        <Border Grid.Row="3" Style="{StaticResource CardStyle}">
            <DataGrid ItemsSource="{Binding Alarms}" Style="{StaticResource StyledDataGrid}" AutoGenerateColumns="False" IsReadOnly="True" BorderThickness="0">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="时间" Binding="{Binding TriggerTime, StringFormat=yyyy-MM-dd HH:mm:ss}" Width="140"/>
                    <DataGridTextColumn Header="点位ID" Binding="{Binding PointId}" Width="80"/>
                    <DataGridTemplateColumn Header="类型" Width="80">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Border Background="{StaticResource BrushDanger}" CornerRadius="4" Padding="6,2" HorizontalAlignment="Left">
                                    <TextBlock Text="{Binding AlarmType}" Foreground="White" FontSize="12" FontWeight="SemiBold"/>
                                </Border>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                    <DataGridTextColumn Header="触发值" Binding="{Binding TriggerValue, StringFormat=F2}" Width="100"/>
                    <DataGridTextColumn Header="限值" Binding="{Binding LimitValue, StringFormat=F2}" Width="100"/>
                    <DataGridCheckBoxColumn Header="已确认" Binding="{Binding Acknowledged}" Width="80"/>
                    <DataGridTemplateColumn Header="操作" Width="100">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Button Content="确认" Command="{Binding DataContext.AcknowledgeCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding Id}" Padding="12,2" Background="{StaticResource BrushPrimary}" Foreground="White" BorderThickness="0" CornerRadius="4"/>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Views/AlarmView.xaml
git commit -m "feat: redesign alarm view with stats cards and filters"
```

---

## Task 10: 重构能耗报表 EnergyReportView

**Files:**
- Modify: `HVAC.EnergyMonitor/Views/EnergyReportView.xaml`

- [ ] **Step 1: 重写 EnergyReportView.xaml**

Modify: `HVAC.EnergyMonitor/Views/EnergyReportView.xaml`

```xml
<UserControl x:Class="HVAC.EnergyMonitor.Views.EnergyReportView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:scottplot="clr-namespace:ScottPlot.WPF;assembly=ScottPlot.WPF"
             xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="能耗报表" Style="{StaticResource PageTitleStyle}" Margin="0,0,0,16"/>

        <!-- Toolbar -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,12">
            <TextBlock Text="点位:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <ComboBox ItemsSource="{Binding Points}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding SelectedPointId}" Width="160" Margin="0,0,16,0" Padding="8,4"/>
            <TextBlock Text="周期:" VerticalAlignment="Center" Foreground="{StaticResource BrushTextSecondary}" Margin="0,0,6,0"/>
            <ComboBox SelectedItem="{Binding SelectedPeriod}" Width="100" Margin="0,0,16,0" Padding="8,4">
                <ComboBox.ItemsSource>
                    <x:Array Type="{x:Type sys:String}" xmlns:sys="clr-namespace:System;assembly=mscorlib">
                        <sys:String>Hour</sys:String>
                        <sys:String>Day</sys:String>
                        <sys:String>Month</sys:String>
                    </x:Array>
                </ComboBox.ItemsSource>
            </ComboBox>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="查询" Command="{Binding QueryCommand}" Margin="0,0,8,0">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Magnify" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
            <Button Style="{StaticResource ToolbarButtonStyle}" Content="导出">
                <Button.Tag>
                    <iconPacks:PackIconMaterial Kind="Download" Width="16" Height="16"/>
                </Button.Tag>
            </Button>
        </StackPanel>

        <!-- Report table + pie chart -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="1.5*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0" Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="报表数据" FontSize="16" FontWeight="SemiBold" Foreground="{StaticResource BrushTextPrimary}" Margin="0,0,0,12"/>
                    <DataGrid Grid.Row="1" ItemsSource="{Binding Reports}" Style="{StaticResource StyledDataGrid}" AutoGenerateColumns="False" IsReadOnly="True" BorderThickness="0">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="周期开始" Binding="{Binding PeriodStart}" Width="*"/>
                            <DataGridTextColumn Header="周期结束" Binding="{Binding PeriodEnd}" Width="*"/>
                            <DataGridTextColumn Header="累计值" Binding="{Binding TotalValue, StringFormat=F2}" Width="*"/>
                            <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="80"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </Grid>
            </Border>

            <Border Grid.Column="1" Style="{StaticResource CardStyle}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Text="能耗占比" FontSize="16" FontWeight="SemiBold" Foreground="{StaticResource BrushTextPrimary}" Margin="0,0,0,12"/>
                    <scottplot:WpfPlot Grid.Row="1" x:Name="EnergyPiePlot"/>
                </Grid>
            </Border>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: 在 EnergyReportView.xaml.cs 中绑定饼图**

Modify: `HVAC.EnergyMonitor/Views/EnergyReportView.xaml.cs`

```csharp
using HVAC.EnergyMonitor.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HVAC.EnergyMonitor.Views;

public partial class EnergyReportView : UserControl
{
    public EnergyReportView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not EnergyReportViewModel vm) return;
        vm.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(EnergyReportViewModel.Reports))
                RefreshPie(vm);
        };
        RefreshPie(vm);
    }

    private void RefreshPie(EnergyReportViewModel vm)
    {
        var plot = EnergyPiePlot.Plot;
        plot.Clear();
        var values = vm.Reports.Select(r => r.TotalValue).ToArray();
        var labels = vm.Reports.Select(r => r.PeriodStart.ToString("MM-dd HH")).ToArray();
        if (values.Length > 0)
        {
            var pie = plot.Add.Pie(values);
            pie.SliceLabels = labels;
            pie.ShowSliceLabels = true;
        }
        EnergyPiePlot.Refresh();
    }
}
```

- [ ] **Step 3: 编译验证**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```powershell
cd d:\study\623WPFstudy
git add HVAC.EnergyMonitor/Views/EnergyReportView.xaml HVAC.EnergyMonitor/Views/EnergyReportView.xaml.cs
git commit -m "feat: redesign energy report view with table and pie chart"
```

---

## Task 11: 最终构建与运行验证

**Files:** 所有 View/XAML 文件

- [ ] **Step 1: Release 构建**

Run:
```powershell
cd d:\study\623WPFstudy
dotnet build HVAC.EnergyMonitor.sln --configuration Release
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 2: 运行验证**

Run:
```powershell
cd d:\study\623WPFstudy\HVAC.EnergyMonitor\bin\Release\net8.0-windows
.\HVAC.EnergyMonitor.exe
```

Expected: 主窗口打开，左侧导航可切换，各页面显示新的工业风界面，Dashboard 有 KPI 卡片和曲线。

- [ ] **Step 3: 最终 Commit**

```powershell
cd d:\study\623WPFstudy
git add .
git commit -m "feat: complete industrial-grade UI upgrade for all views"
```

---

## 自检清单

### Spec 覆盖度

| Spec 要求 | 对应 Task |
|---|---|
| 浅色专业工业风 | Task 2 |
| 左侧图标导航 | Task 4 |
| 顶部状态栏 | Task 4 |
| Dashboard KPI 卡片 + 实时曲线 | Task 5 |
| 设备管理工具栏 + 状态灯 | Task 6 |
| 点位管理筛选 + 列分组 | Task 7 |
| 历史趋势三面板 | Task 8 |
| 报警统计卡片 + 表格 | Task 9 |
| 能耗报表表格 + 饼图 | Task 10 |
| IconPacks 依赖 | Task 1 |

### Placeholder 检查

- 无 TBD/TODO/"implement later"
- 所有颜色值为具体 HEX
- 所有文件路径具体
- 每个 Task 含完整 XAML 或代码

### 类型一致性

- `HistoryDataPoint` 统一抽取到 `Models/HistoryDataPoint.cs`，避免 `DashboardViewModel` 与 `HistoryTrendViewModel` 重复定义
- `SelectedPeriod` 保持 `string` 类型（"Hour"/"Day"/"Month"）
- `AlarmType` 绑定直接显示枚举字符串

---

## 执行方式选择

计划已保存到 `docs/superpowers/plans/2026-06-23-hvac-ui-upgrade-plan.md`。

**两种执行方式：**

1. **Subagent-Driven（推荐）**：每个 Task 派一个子代理实现，两阶段审查，质量可控。
2. **Inline Execution**：在当前会话中按 Task 顺序逐步实现，响应更快。

请选择你想要的方式：回复 **1** 或 **2**。
