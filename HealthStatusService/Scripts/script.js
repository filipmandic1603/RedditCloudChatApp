document.addEventListener('DOMContentLoaded', function () {
    var uptime24hElement = document.querySelector('.uptime-24h');
    var uptime1hElement = document.querySelector('.uptime-1h');

    var uptimePercentage24h = uptime24hElement.dataset.uptime24h;
    var uptimePercentage1h = uptime1hElement.dataset.uptime1h;

    var ctx24h = document.getElementById('uptimeChart24h').getContext('2d');
    var ctx1h = document.getElementById('uptimeChart1h').getContext('2d');

    var data24h = {
        labels: ['Uptime'],
        datasets: [{
            label: 'Last 24 hours',
            data: [uptimePercentage24h],
            backgroundColor: ['rgba(54, 162, 235, 0.2)'],
            borderColor: ['rgba(54, 162, 235, 1)'],
            borderWidth: 1
        }]
    };

    var data1h = {
        labels: ['Uptime'],
        datasets: [{
            label: 'Last 1 hour',
            data: [uptimePercentage1h],
            backgroundColor: ['rgba(255, 206, 86, 0.2)'],
            borderColor: ['rgba(255, 206, 86, 1)'],
            borderWidth: 1
        }]
    };

    var options = {
        scales: {
            y: {
                beginAtZero: true
            }
        }
    };

    new Chart(ctx24h, {
        type: 'bar',
        data: data24h,
        options: options
    });

    new Chart(ctx1h, {
        type: 'bar',
        data: data1h,
        options: options
    });
});
