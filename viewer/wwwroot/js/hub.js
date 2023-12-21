var hubConnection;
var hubSessionId;
var hubConnectionId;

var watcherClear = function () {
  $("#grid-events").find("tr:gt(0)").remove();
  $("#grid-events").hide();
}

var watcherAddEvent = function (id, eventType, subject, eventTime, data) {

  console.log("event added:", id);

  var context = {
    gridEventType: eventType,
    gridEventTime: eventTime,
    gridEventSubject: subject,
    gridEventId: id,
    gridEvent: data
  };
  var source = document.getElementById('event-template').innerHTML;
  var template = Handlebars.compile(source);
  var html = template(context);

  $("#grid-events").show();
  $('#grid-event-details').prepend(html);
}

var watcherInit = function () {

  console.log("init hub");

  $("#grid-events").hide();
  var clearEvents = document.getElementById('clear-events');
  clearEvents.addEventListener('click', function () {
    watcherClear();
  });

  // build connection

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("hubs/gridevents")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();
  hubSessionId = "";

  // ensure handlers registered before connection

  hubConnection.on('identification', function (identity) {
    console.log(identity);
    hubSessionId = identity.sessionId;
    hubConnectionId = identity.connectionId;
  });

  hubConnection.on('gridupdate', function (evt) {
    console.log(evt);
    watcherAddEvent(evt.id, evt.type, evt.subject, evt.time, evt.data);
  });

  async function startHub() {
    try {
      await hubConnection.start();
      console.assert(connection.state === signalR.HubConnectionState.Connected);
      console.log("hub connected.");
      await hubConnection.invoke("BindSession", hubSessionId);
      console.log("session bound.");
    } catch (err) {
      console.assert(connection.state === signalR.HubConnectionState.Disconnected);
      console.error(err);
      setTimeout(() => startHub(), 5000);
    }
  };

  hubConnection.onreconnecting(error => {
    console.log(hubConnection.state);
    console.warn(`Connection lost due to error '${ error }'. Reconnecting..`);
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
