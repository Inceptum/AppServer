define([
  'jQuery',
  'Underscore',
  'Backbone',
  'text!templates/configurations/list.html'
], function($, _, Backbone,  configurationListTemplate){
  var configurationListView = Backbone.View.extend({
    tagName: "div",
    initialize: function(){
      _.bindAll(this, "render");
      this.collection.bind("reset", this.render,this);
    },

    events: {
            "click #update"              : "update"
          },
    update: function(){
        this.collection.fetch();
    },
    render: function(){
      var data = {
        configurations: this.collection.models,
        _: _
      };
      var compiledTemplate = _.template( configurationListTemplate, data );
        $(this.el).html( compiledTemplate );
      return this;
    }
  });
  return configurationListView;
});
