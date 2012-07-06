define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/home/message.html'
], function ($, _, Backbone, messageTemplate) {

    var messageView = Backbone.View.extend({

        template:_.template(messageTemplate),

        events:{
            "click .close":"close"
        },

        render:function () {

            var data = {
                message:this.model,
                _:_
            };
            this.$el.html(this.template(data)).addClass('popup');
            return this;
        }
    });

    return messageView;
});
