import sys
from tokenizer import Tokenizer
from group import group
from event import event

class Parser:
    path = ""
    hookupSymbolTable = {}
    groupList = []
    eventList = []
    #groupDefList = []
    
    def __init__(self,path,filename):
        self.path = path
        self.tokenizerObject = Tokenizer(path + filename)
        self.parse(self.tokenizerObject.getnextToken())
    
    def parse(self,token):
        if token.startswith("level"):
            Parser.levelSpec(self,token)
        elif token == "include":
            Parser.inclSpec(self,token)
        elif token == "group":
            Parser.groupSpec(self,token)
        elif token == "event":
            #print(token+ "\n")
            Parser.eventSpec(self,token)
        elif token == "EOF":
            print("")
            #print("End of File Reached\n")
        else:
            #print("Not Finding Valid Token")
            self.parse(self.tokenizerObject.getnextToken())
            
    def groupSpec(self,token):
        #print("Group Spec Found")
        groupDefList = []
        while token != "SEMICOLON":
            groupDefList.append(token)
            token = self.tokenizerObject.getnextToken()
        groupDefList.append("SEMICOLON")
        #print(groupDefList)
        groupObj = group(groupDefList)
        self.groupList.append(groupObj)
        #print(self.groupList[0].getGroupName())
        self.parse(self.tokenizerObject.getnextToken())
        
    def eventSpec(self,token):
        eventDefList = []
        while token != "end":
            eventDefList.append(token)
            token = self.tokenizerObject.getnextToken()
        eventDefList.append("end")
        eventDefList.append("SEMICOLON")
        #print(eventDefList)
        eventObj = event(eventDefList)
        self.eventList.append(eventObj)
        #print(self.eventList)
        self.parse(self.tokenizerObject.getnextToken())
        
    def parseInclude(self,filename):
        #print(filename)
        self.includeTokenizerObject = Tokenizer(self.path + "\\" + filename)
        #print(self.includeTokenizerObject.getTokenList())
        tokenList = self.includeTokenizerObject.getTokenList()
        counter = 0
        token = tokenList[counter]
        while token != "EOF":
            if token == "COLON":
                self.hookupSymbolTable[tokenList[counter - 1]] = tokenList[counter + 2]
            counter = counter + 1
            token = tokenList[counter]
        #print(self.hookupSymbolTable)
        
    def inclSpec(self,token):
        #print("Hook Up file found")
        while token != "SEMICOLON":
            #self.parseInclude(token)
            token = self.tokenizerObject.getnextToken()       
            if token == "SPACE" or token == "SEMICOLON":
                continue
            else:
                #print(token)
                self.parseInclude(token)
        #print(self.tokenizerObject.getnextToken())
        Parser.parse(self,self.tokenizerObject.getnextToken())
                
                
        
    
    def levelSpec(self,token):
        if token == "level_0":
            #print("Level 0 Event Definition Found\n")
            self.parse(self.tokenizerObject.getnextToken())
        elif token == "level_1":
            #print("Level 1 Event Definition Found\n")
            Parser.parse(self,self.tokenizerObject.getnextToken())
        elif token == "level_2":
            #print("Level 2 Event Definition Found\n")
            Parser.parse(self,self.tokenizerObject.getnextToken())
        else:
            print("Unknown Event Definition .. Exiting\n")
    
    def getGroupName(self,eventInput):
        for event in self.eventList:
            if eventInput in event.getEventSignalDictionary():
                return event.getGroupName()
    
    def getLogicalLineList(self,groupName):
        for group in self.groupList:
            if groupName == group.getGroupName():
                return group.getLogicalLines()    

def getX(parserObject,signame,signal):
    #print(signame)
    #print(signame,signal)
    #print(parserObject.getGroupName(signame),parserObject.hookupSymbolTable[parserObject.getGroupName(signame)])
    X = 0
    for logicalLine in parserObject.getLogicalLineList(parserObject.getGroupName(signame)):
        #print(signame,parserObject.hookupSymbolTable[logicalLine],signal)
        physicalLine = parserObject.hookupSymbolTable[logicalLine]
        X |= 1 << int(physicalLine)-1
        #print(X)
    return X
    #print(parserObject.hookupSymbolTable)
    
    
def getT(parserObject,signame,signal):
    #print(signame,signal)
    T = 0
    for logicalLine in parserObject.getLogicalLineList(parserObject.getGroupName(signame)):
        #print(signame,parserObject.hookupSymbolTable[logicalLine],signal)
        physicalLine = parserObject.hookupSymbolTable[logicalLine]        
        if signal == "R" or signal == "F":
            T |= 1 << int(physicalLine)-1
        #print(T)
    return T
            
    
def getS(parserObject,signame,signal):
    #print(signame,signal)
    S = 0
    for logicalLine in parserObject.getLogicalLineList(parserObject.getGroupName(signame)):
        #print(signame,parserObject.hookupSymbolTable[logicalLine],signal)
        physicalLine = parserObject.hookupSymbolTable[logicalLine]
        if signal == "R":
            #print("Rising")
            S |= 1 << int(physicalLine)-1
        #print(S)
    return S

def genUSBPacket(parserObject):
    USBPACKET = "USB_DOWNLOAD_COMMAND_HEADER"
    for events in parserObject.eventList:
        eventInfo = ""
        eventInfo += events.getGroupName() + "\t"
        for signals in events.eventsignalDict:
            #print(signals)
            signalInfo = ""
            for signalDefinition in events.eventsignalDict[signals]:
                signalInfo += signalDefinition
                X = getX(parserObject,signals,signalDefinition)
                T = getT(parserObject,signals,signalDefinition)
                S = getS(parserObject,signals,signalDefinition)
                print(signals + "\t" + str(X) + "\t" + str(T) + "\t" + str(S))
                #print(S)
            eventInfo += signals + signalInfo + "\t"
        #print(eventInfo)
        
if  __name__ == "__main__":    
    #print(sys.argv[1])
    if len(sys.argv) < 3:
        print("Insufficient Arguments")
        print("Usage: Python Parser.py [path] [filename]")
        exit()
    path = sys.argv[1]
    filename = sys.argv[2]
    #print(path + "\\" + filename)
    parserObject = Parser(path,filename)
    genUSBPacket(parserObject)
    print("Parse complete.")