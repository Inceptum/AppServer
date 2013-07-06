define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/confirm.html'],
    function($, Backbone, _,  template,confirmView){
        var View = Backbone.View.extend({
            className: "modal hide ",
            events: {
                'click .confirm'	: '_handleConfirm',
                'click .cancel'	: '_handleCancel'
            },
            _handleConfirm: function(event){
                event.preventDefault();
                this.confirm();
            },
            _handleCancel: function(event){
                event.preventDefault();
                this.cancel();
            },
            confirm: function(){
                this.hide();
                this.deferred.resolve();
            },
            cancel: function(){
                this.hide();
                this.deferred.reject();
            },
            getTemplateData: function(){
                return {
                    title: this.options.title,
                    body: this.options.body,
                    confirm_text: this.options.confirm_text || 'Confirm',
                    cancel_text: this.options.cancel_text || 'Cancel'
                };
            },
            open: function(options){
                var params= _.extend( {
                    confirm_text: 'Confirm',
                    cancel_text: 'Cancel'
                },options);
                var deferred = this.deferred = new $.Deferred;
                this.template = _.template( template, params );
                $(this.el).html(this.template).modal({backdrop:true,show:true});

                $('body').append(this.el);

                this.$el.modal({
                    keyboard:false,
                    backdrop:false
                });

                $('.modal-backdrop').off('click');

                return deferred;
            },
            hide: function(){
                this.$el.modal('hide');
            }
        });

        return new View();
    });
