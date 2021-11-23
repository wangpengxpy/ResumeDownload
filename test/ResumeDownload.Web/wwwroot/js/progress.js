(function (window, $) {
    "use strict";

    var _hubConnection;

    OnInit();

    function OnInit() {

        _hubConnection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/progress")
            .configureLogging(signalR.LogLevel.Debug)
            .build();


        _hubConnection.onclose(e => {
            _hubConnection.stop();
            if (e) {
                console.log(e.message);
                console.log("Hub connection closed due to the following error" + e.name);
                connect();
            } else {
                console.log('Hub connection closed');
            }
        });

        connect();

        _hubConnection.on('progress', (data) => {

            $('#progress').attr('aria-valuenow', data.percentage);
            $('#progress').css('width', data.percentage + '%');

            $('#percentage').html(data.percentage + '%');

            $('#size').html('下载速度：' + data.downloadRate + '  ' + data.downloadSize + '/' + data.totalSize);
        });

    }

    function connect() {
        _hubConnection.start().then(() => {
            console.log('Hub connection started');
        }).catch(err => {
            console.error(err.stack.toString());
            return false;
        });
    }

}(this, jQuery));