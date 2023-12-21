var hubConnection;

var watcherClear = function (detailsId) {
  $("#" + detailsId).children().remove();
}

var watcherAddEvent = function (templateId, detailsId, id, eventType, subject, eventTime, data) {

  console.log("event added:", id);
  var detailsElem = $('#' + detailsId);
  var index = detailsElem.children().length;

  var context = {
    gridEventType: eventType,
    gridEventTime: eventTime,
    gridEventSubject: subject,
    gridEventId: id + index,
    gridEvent: data
  };
  var source = document.getElementById(templateId).innerHTML;
  var template = Handlebars.compile(source);
  var html = template(context);

  detailsElem.prepend(html);
}

var watcherInit = function (templateId, detailsId, clearId) {

  console.log("init hub");

  var clearEvents = document.getElementById(clearId);
  clearEvents.addEventListener('click', function () {
    watcherClear(detailsId);
  });

  // build connection

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("hubs/gridevents")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

  // ensure handlers registered before connection

  hubConnection.on('gridupdate', function (evt) {
    console.log(evt);
    watcherAddEvent(templateId, detailsId, evt.id, evt.type, evt.subject, evt.time, evt.data);
  });

  async function startHub() {
    try {
      await hubConnection.start();
      console.assert(connection.state === signalR.HubConnectionState.Connected);
      console.log("hub connected.");
    } catch (err) {
      console.assert(connection.state === signalR.HubConnectionState.Disconnected);
      console.error(err);
      setTimeout(() => startHub(), 5000);
    }
  };

  hubConnection.onreconnecting(error => {
    console.log(hubConnection.state);
    console.warn(`Connection lost due to error '${error}'. Reconnecting..`);
  });

  hubConnection.onreconnected(connectionId => {
    console.log(hubConnection.state);
    console.log(`Connection reestablished. Connected with connectionId "${connectionId}".`);
  });

  hubConnection.onclose(error => {
    console.log(hubConnection.state);
    console.error(`Connection closed due to error "${error}". Try refreshing this page to restart the connection.`);
    alert("Hub connection lost. Please refresh page.");
  });

  startHub();

};