define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/bundle.html',
    'views/alerts',
    'jsonlint',
    'codemirror',
    'codemirror/mode/javascript/javascript',
    'codemirror/addon/search/search',
    'codemirror/addon/search/matchesonscrollbar',
	'libs/prettify',
    'shortcut'],
    function ($, Backbone, _, template, alerts, jsonlint, CodeMirror) {
        var View = Backbone.View.extend({
            el:'#content',
            initialize:function () {
                this.isFullScreen= false;
                this.isPreview= false;
                this.content= "";
                this.configuration=this.options.configuration;
                _.bindAll(this, "verify", "jumpToLine", "save","togglePreview","toggleFullScreen","switchToPreview");
            }, events:{
                "click #verify":"verify",
                "click #save":"save",
                "change #inputName":"change" ,
                "click #preview":"togglePreview",
                "click #fullScreen":"toggleFullScreen"
            },
            togglePreview:function () {
                if(this.isPreview){
                    this.codeMirror.setValue(this.content);
                    this.codeMirror.setOption("readOnly",false);
                    this.isPreview=false;
                    $("#preview").removeClass("active");
                    $("#verify").removeClass("disabled");
                }
                else{
                    if (!this.verify())
                        return;
                    this.content=this.codeMirror.getValue();
                    this.switchToPreview();
                }
            },
            switchToPreview:function(){
                var parentContent = (this.model.attributes.parent!=null)
                    ?this.configuration.getBundle(this.model.attributes.parent).attributes.content
                    :{};
                var merged = $.extend(true,{},JSON.parse(parentContent), JSON.parse(this.codeMirror.getValue()));
                this.codeMirror.setValue(JSON.stringify(merged, null, "  "));
                this.codeMirror.setOption("readOnly",true);
                this.isPreview=true;
                $("#preview").addClass("active");
                $("#verify").addClass("disabled");

            },
            toggleFullScreen: function () {
                var wrap = this.codeMirror.getWrapperElement();
                if (!this.isFullScreen) {
                    wrap.className += " CodeMirror-fullscreen";
                    $(".CodeMirror-fullscreen")
                        .height( window.innerHeight || (document.documentElement || document.body).clientHeight + "px")
                        .css("overflow","hidden");
                    document.documentElement.style.overflow = "hidden";
                    this.isFullScreen= true;
                    this.codeMirror.focus();
                } else {
                    $(".CodeMirror-fullscreen")
                        .height("")
                        .css("overflow","");
                    wrap.className = wrap.className.replace(" CodeMirror-fullscreen", "");
                    this.isFullScreen= false;
                }
                this.codeMirror.refresh();
            },
            'change':function(event){
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
                if (this.verify()) {
                    var self = this;
                    var action=this.model.isNew()?"create":"update";
                    var content=this.isPreview?this.content:this.codeMirror.getValue()
                    this.model.set("pureContent",content);

                    var options = {
                        success: function (model) {
                            self.content=model.get("pureContent");
                            if(self.isPreview){
                                self.switchToPreview();
                            }
                            else
                                self.codeMirror.setValue(model.get("pureContent"));
                            alerts.show({type:"info",text:"Bundle '"+model.get("name")+"' "+action+"d"});
                            self.navigate('#/configurations/'+model.get("configuration")+"/bundles/"+model.id, true);
                        },
                        error: function (model,response) {
                            var bundleId=action==="update"?"'"+model.id+"'":"";
                            alerts.show({type:"error",text:"Failed to "+action+" bundle "+bundleId+". "+JSON.parse(response.responseText).Error});
                        },
                        wait:true
                    };


                    if(this.model.isNew())
                        this.model=this.configuration.createBundle(this.model,options);
                    else
                        this.model.save(null, options);
                }
            },
            verify:function () {
                if(this.isPreview)
                    return;
                if (this.errorLine){
                    this.codeMirror.removeLineClass(this.errorLine, "error", "error");
                    console.log(this.errorLine)  ;
                }
                this.verificationErrors.text("").hide();
                this.errorLine = undefined;

                var json = this.codeMirror.getValue();
                var lint=JSONLint(json, {comments:true} )
                if(lint.error){
                    this.errorLine = lint.line-1;
                    this.verificationErrors.html("<strong>Error ("+lint.line+","+lint.character+"):</strong> " + lint.error).show();
                    this.codeMirror.addLineClass(this.errorLine, "error", "error");
                    this.jumpToLine(this.errorLine);
                    return false;
                } else{
                    this.codeMirror.setValue(JSON.stringify(JSON.parse(json), null, "  "));
                    return true;
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
                var self=this;
                this.codeMirror = CodeMirror.fromTextArea($(this.el).find("#code").get()[0], {
                    lineNumbers:true,
                    matchBrackets:true,
                    mode: "application/json",
                    lineNumberFormatter:function (integer) {
                        return integer + ".";
                    }
                });
                CodeMirror.on(window, "resize", function() {
                    var showing = $(".CodeMirror-fullscreen");
                    if (!showing) return;
                    showing.height( window.innerHeight || (document.documentElement || document.body).clientHeight + "px");
                });
                this.verificationErrors = $("#verificationErrors").hide();
                prettyPrint();
                shortcut.add("F11", this.toggleFullScreen);
                shortcut.add("ctrl+p", this.togglePreview);
                shortcut.add("ctrl+alt+f", this.verify);
                shortcut.add("ctrl+s", this.save);
            },
            dispose: function () {
                shortcut.remove("F11");
                shortcut.remove("ctrl+p");
                shortcut.remove("ctrl+alt+f");
                shortcut.remove("ctrl+s");
            }
        });

        return View;
    });
