#this is temporary solution. Workaround for the bug in OW where scoped dependencies are not updated
o add-wrap  Mono.Cecil -scope srv
o add-wrap  openwrap -anchored  -scope srv
o add-wrap  IbRasta -scope srv
o add-wrap  protobuf-net -scope srv
o update-wrap 