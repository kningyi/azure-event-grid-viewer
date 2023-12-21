var hubConnection;

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

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("hubs/gridevents")
    .configureLogging(signalR.LogLevel.Information)
    .build();

  hubConnection.start().catch(err => console.error(err.toString()));
  hubConnection.on('gridupdate', function (evt) {
    console.log(evt);
    watcherAddEvent(evt.id, evt.type, evt.subject, evt.time, evt.data);
  });
};
