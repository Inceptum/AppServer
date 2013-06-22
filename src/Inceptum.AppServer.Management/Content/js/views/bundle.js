define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/bundle.html',
    'views/alerts','libs/prettify',
    'codemirrorjs', 'libs/jsonlint'],
    function ($, Backbone, _, template,alerts) {
        var View = Backbone.View.extend({
            el:'#content',
            initialize:function () {
                this.configuration=this.options.configuration;
                _.bindAll(this, "verify", "jumpToLine", "save");
            }, events:{
                "click #verify":"verify",
                "click #save":"save",
                "change #inputName":"change"
            },
            'change':function(){
                // Apply the change to the model
                var target = event.target;
                var change = {};
                if(event.target.type=="checkbox")
                    change[target.name] = target.value!="";
                else
                    change[target.name] = target.value;
                this.model.set(change);
            },
            save:function () {
                if(this.verify()){
                    var self = this;
                    var action=this.model.isNew()?"create":"update";
                    this.model.set("Content",this.codeMirror.getValue());
                    var options = {
                        success: function (model) {
                            self.codeMirror.setValue(model.get("Content"));
                            alerts.show({type:"info",text:"Bundle '"+model.get("Name")+"' "+action+"d"});

                            self.navigate('#/configurations/'+model.get("Configuration")+"/bundles/"+model.id, true);
                        },
                        error: function (model,response) {
                            var bundleId=action==="update"?"'"+model.id+"'":"";
                            alerts.show({type:"error",text:"Failed to "+action+" bundle "+bundleId+". "+JSON.parse(response.responseText).Error});
                        },
                        wait:true
                    };

                    if(this.model.isNew())
                        this.configuration.createBundle(this.model,options);
                    else
                        this.model.save(null, options);
                }
            },
            verify:function () {
                try {
                    if (this.errorLine)
                        this.codeMirror.setLineClass(this.errorLine, null, null);
                    var result = jsonlint.parse(this.codeMirror.getValue());
                    this.codeMirror.setValue(JSON.stringify(result, null, "  "));
                    this.verificationErrors.text("").hide();
                    this.errorLine = undefined;
                    return true;
                } catch (ex) {
                    this.errorLine = (/Parse error on line (\d+)/g).exec(ex.message)[1] * 1 - 1;
                    this.verificationErrors.html("<strong>Error:</strong> " + ex.message).show();
                    this.codeMirror.setLineClass(this.errorLine, "error", "error");
                    this.jumpToLine(this.errorLine);
                    return false;
                }
            },
            jumpToLine:function (i) {
                this.codeMirror.setCursor(i);
                window.setTimeout(function () {
                    var line = $('.CodeMirror-lines .error');
                    var h = line.parent();
                    $('.CodeMirror-scroll').scrollTop(0).scrollTop(line.offset().top - $('.CodeMirror-scroll').offset().top - Math.round($('.CodeMirror-scroll').height() / 2));
                }, 200);
            },
            render:function () {
                this.template = _.template(template, { model:this.model.toJSON() });
                $(this.el).html(this.template);
                this.codeMirror = CodeMirror.fromTextArea($(this.el).find("#code").get()[0], {
                    lineNumbers:true,
                    matchBrackets:true,
                    mode:"javascript",
                    lineNumberFormatter:function (integer) {
                        return integer + ".";
                    }
                });
                this.verificationErrors = $("#verificationErrors").hide();
                //      $(".CodeMirror").addClass("well");
                prettyPrint();
            }
        });

        return View;
    });
