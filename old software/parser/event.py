

class event:
    groupName = ""
    eventsignalDict ={}
    semicoloncounter = 0
    eventdef = []
    
    
    
    def __init__(self,eventdef):
        #groupName = ""
        #eventsignalDict = {}
        #eventdef = []
        #semicoloncounter = 0
        self.eventdef = eventdef
        for token in eventdef:
            if token == "SEMICOLON":
                self.semicoloncounter += 1
        if self.semicoloncounter > 1:
            self.multiEventDef()
        else:
            self.singleEventDef()
        
    def multiEventDef(self):
        #print("Multi Event Definition Found\n")
        self.findGroupName()
        self.findEventSignalMap()
        
    def singleEventDef(self):
        #print("Single Event Definition Found\n")
        self.findGroupName()
        self.findEventSignalMap()
        #self.findSignal()
    
    def findEventSignalMap(self):
        self.eventsignalDict = {}
        counter = 0
        signalList = []
        eventList = []
        token = self.eventdef[counter]
        while token != "end":
            if token == "EQUAL":
                eventName = self.eventdef[counter -2]
                #eventList.append(self.eventdef[counter - 2])
                signalList = []
                while token != "CLOSEBRACKET":
                    if token != "COMMA" and token != "OPENBRACKET" and token != "EQUAL" and token != "SPACE":
                        signalList.append(token)
                    counter = counter + 1
                    token = self.eventdef[counter]
                self.eventsignalDict[eventName] = signalList       
            counter = counter + 1
            token = self.eventdef[counter]
        #print(self.eventsignalDict)
        #print(inBracketList)
        #print(eventList)
        
    def findGroupName(self):
        self.groupName = ""
        counter = 0
        token = self.eventdef[counter]
        while token != "CLOSEBRACKET" :
            if token != "event" and token != "OPENBRACKET":
                self.groupName = token
            counter = counter + 1
            token = self.eventdef[counter]
        #print(self.groupName)
        
    def getGroupName(self):
        return self.groupName
    
    def getEventName(self):
        return self.eventName
    
    def getEventSignalDictionary(self):
        return self.eventsignalDict