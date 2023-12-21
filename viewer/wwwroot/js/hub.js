var hubConnection;

var watcherClear = function (detailsId) {
  $("#" + detailsId).children().remove();
}

var watcherAddEvent = function (templateId, detailsId, id, eventType, subject, eventTime, data) {

  console.log("event added:", id);
  var detailsElem = document.getElementById(detailsId);
  var index = detailsElem.children.length;

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

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("hubs/gridevents")
    .configureLogging(signalR.LogLevel.Information)
    .build();

  hubConnection.start().catch(err => console.error(err.toString()));
  hubConnection.on('gridupdate', function (evt) {
    console.log(evt);
    watcherAddEvent(templateId, detailsId, evt.id, evt.type, evt.subject, evt.time, evt.data);
  });
};
