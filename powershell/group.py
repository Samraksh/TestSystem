

class group:
    name = ""
    logicalLine = []
    groupdef = ""
    keywordlist = ['SPACE','COMMA','SEMICOLON','group','EQUAL','CLOSEBRACKET','OPENBRACKET']
    
    def __init__(self,groupdef):
        #print("In Group Def")
        self.groupdef = groupdef
        self.parse(groupdef)
    
    def parse(self,groupdef):
        self.findGroupName(groupdef)
        self.findLogicalLineList(groupdef)
        
        
    def findLogicalLineList(self,groupdef):
        self.logicalLine = []
        counter = 0
        token = groupdef[counter]
        while token != "SEMICOLON":
            if token not in self.keywordlist and token != self.name:
                self.logicalLine.append(token)
            counter = counter + 1
            token = groupdef[counter]
        #print(self.logicalLine)
    
    def findGroupName(self,groupdef):
        counter = 0
        token = groupdef[counter]
        while token != "EQUAL":
            if token != "group" and token != "SPACE":
                self.name = token
                groupName = token 
            counter = counter + 1
            token = groupdef[counter]
        #print(self.name)
    
    def getGroupName(self):
        return self.name
        
    
    def getLogicalLines(self):
        return self.logicalLine
        