// Set new default font family and font color to mimic Bootstrap's default styling
Chart.defaults.global.defaultFontFamily = 'Nunito', '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
Chart.defaults.global.defaultFontColor = '#858796';

// Pie Chart Example
var ctx = document.getElementById("myPieChart");
var data = typeof pieChartData !== "undefined" ? pieChartData : [30, 15, 10];
var myPieChart = new Chart(ctx, {
    type: 'pie',
    data: {
        labels: ["Matched Balance (Rule Based)", "Matched Balance (AI)", "Unmatched"],
        datasets: [{
            data: data,
            backgroundColor: ['#1cc88a', '#4e73df', '#e74a3b'],
            hoverBackgroundColor: ['#1cc88a', '#4e73df', '#e74a3b'],
            hoverBorderColor: "rgba(234, 236, 244, 1)",
        }],
    },
    options: {
        maintainAspectRatio: false,
        tooltips: {
            backgroundColor: "rgb(255,255,255)",
            bodyFontColor: "#858796",
            borderColor: '#dddfeb',
            borderWidth: 1,
            xPadding: 15,
            yPadding: 15,
            displayColors: false,
            caretPadding: 10,
        },
        legend: {
            display: true
        }
        // cutoutPercentage removed for pie chart
    },
});