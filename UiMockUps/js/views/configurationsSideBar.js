define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/configuration/configurationsSideBar.html',
    'collections/configurations',
    'views/alerts'],
    function($, Backbone, _, template,Configurations,alerts){
        var View = Backbone.View.extend({
            el:'#sidebar',
            initialize: function(options){
                this.activeItem=options.active;
                this.template = _.template( template, { model: Configurations.toJSON() } );
            },
            events:{
                "click .create":"createConfig"
            },
            createConfig:function(e){
                e.preventDefault();
                var self = this;
                bootbox.prompt("Configuration Name", function(result) {
                    if(result){
                        Configurations.create({name: result},{
                            wait: true,
                            success:function(){
                                alerts.show({
                                    type:"info",
                                    text:"Configuration '"+result+"' created."});
                                self.navigate("#configurations/"+result);
                            },
                            error:function(model,response){
                                alerts.show({
                                    type:"error",
                                    text:"Failed to create configuration '"+result+"'. "+JSON.parse(response.responseText).Error});
                            }

                        });
                    }
                });
            },
            render: function(){
                $(this.el).html(this.template);
                $(this.el).find('*[data-id="'+this.activeItem+'"]').addClass("active");
            },
            'dispose':function(){
            }
        });

        return View;
    });
/**
 * Created with JetBrains WebStorm.
 * User: knst
 * Date: 07.10.12
 * Time: 16:08
 * To change this template use File | Settings | File Templates.
 */
