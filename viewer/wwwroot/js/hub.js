var hubConnection;

var watcherClear = function () {
  $("#grid-events").find("tr:gt(0)").remove();
  $("#grid-events").hide();
}

var watcherAddEvent = function (id, eventType, subject, eventTime, data) {

  console.log("event added:");
  console.log(data);

  var context = {
    gridEventType: eventType,
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
  hubConnection.on('gridupdate', function (id, eventType, subject, eventTime, data) {
    watcherAddEvent(id, eventType, subject, eventTime, data);
  });
};
