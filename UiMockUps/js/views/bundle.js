define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/bundle.html',
    'libs/prettify',
    'codemirrorjs','libs/jsonlint'],
    function($, Backbone, _,  template){
        var View = Backbone.View.extend({
            el: '#content',
            initialize: function(){
                _.bindAll(this, "verify","jumpToLine");
            },events:{
              "click #verify":"verify"
            },
            verify:function(){
                try
                {
                    if(this.errorLine)
                        this.codeMirror.setLineClass(this.errorLine, null,null);
                    var result = jsonlint.parse(this.codeMirror.getValue());
                        this.codeMirror.setValue(JSON.stringify(result, null, "  "));
                    this.verificationErrors.text("").hide();
                    this.errorLine=undefined;
                }catch(ex){
                    this.errorLine = (/Parse error on line (\d+)/g).exec(ex.message)[1]*1-1;
                    this.verificationErrors.html("<strong>Error:</strong> "+ex.message).show();
                    this.codeMirror.setLineClass(this.errorLine, "error","error");
                    this.jumpToLine(this.errorLine);
                    console.log(ex);
                }
            },
            jumpToLine:function (i) {

            // editor.getLineHandle does not help as it does not return the reference of line.
                this.codeMirror.setCursor(i);
            window.setTimeout(function() {
                var line = $('.CodeMirror-lines .error');
                var h = line.parent();
                $('.CodeMirror-scroll').scrollTop(0).scrollTop(line.offset().top - $('.CodeMirror-scroll').offset().top - Math.round($('.CodeMirror-scroll').height()/2));
               // $('body').scrollTop(0).scrollTop(line.offset().top - $('body').offset().top - Math.round($('body').height()/2));
            }, 200);
        },
            render: function(){
                this.template = _.template( template, { model: this.model.toJSON() } );
                $(this.el).html(this.template);
                this.codeMirror=CodeMirror.fromTextArea($(this.el).find("#code").get()[0], {
                    lineNumbers: true,
                    matchBrackets: true,
                    mode:  "javascript",
                    lineNumberFormatter:function(integer){return integer+".";}
                });
                this.verificationErrors=$("#verificationErrors").hide();
          //      $(".CodeMirror").addClass("well");
                prettyPrint();
            }
        });

        return View;
    });
