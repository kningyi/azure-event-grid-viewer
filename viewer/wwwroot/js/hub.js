var hubConnection;
var hubSessionId;
var connectionAttempts = 0;

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

var watcherAddSubscriber = function (connection, subscriberId) {

  var subscribeDiv = document.getElementById(subscriberId);

  var subscribeInput = document.createElement('input');
  subscribeInput.setAttribute('type', 'text');
  subscribeInput.setAttribute('id', 'subscribeInput');
  subscribeDiv.appendChild(subscribeInput);

  var subscribeButton = document.createElement('button');
  subscribeButton.textContent = 'Subscribe';
  subscribeDiv.appendChild(subscribeButton);
  subscribeButton.addEventListener("click", async (event) => {
    var val = document.getElementById("subscribeInput").value;
    if (val) {
      console.log("Subscribing to", val);
      try {
        await connection.invoke("Subscribe", val);
      }
      catch (e) {
        console.error(e);
      }
    }
    event.preventDefault();
  });

  var unsubButton = document.createElement('button');
  unsubButton.textContent = 'Unsubscribe';
  subscribeDiv.appendChild(unsubButton);
  unsubButton.addEventListener("click", async (event) => {
    var val = document.getElementById("subscribeInput").value;
    if (val) {
      console.log("Unsubscribing from", val);
      try {
        await connection.invoke("Unsubscribe", val);
      }
      catch (e) {
        console.error(e);
      }
    }
    event.preventDefault();
  });
}

var watcherInit = function (templateId, detailsId, subscriberId, clearId) {

  console.log("init hub");

  var clearEvents = document.getElementById(clearId);
  clearEvents.addEventListener('click', function () {
    watcherClear(detailsId);
  });

  // build connection

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("hubs/gridevents")
    .configureLogging(signalR.LogLevel.Information)
    //.withAutomaticReconnect()
    .build();

  // ensure handlers registered before connection

  hubConnection.on('identification', function (identity) {
    console.log(identity);
    hubSessionId = identity.sessionId;
  });

  hubConnection.on('gridupdate', function (evt) {
    console.log(evt);
    watcherAddEvent(templateId, detailsId, evt.id, evt.type, evt.subject, evt.time, evt.data);
  });

  watcherAddSubscriber(hubConnection, subscriberId);

  async function startHub() {
    try {
      connectionAttempts = connectionAttempts + 1;
      await hubConnection.start();
      console.assert(connection.state === signalR.HubConnectionState.Connected, connection.state);
      await hubConnection.invoke("BindSession", hubSessionId);
      console.log("session bound.");
    } catch (err) {
      console.error(err);
      setTimeout(() => startHub(), 5000 + connectionAttempts * 5000);
    }
  };

  /*
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
  */

  hubConnection.onclose(async () => {
    console.log("hub closed, reconnecting..");
    await startHub();
  });

  // establish connection

  startHub();

};