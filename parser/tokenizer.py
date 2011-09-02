import sys


class Tokenizer:
    fileName = ""
    tokenList = []
    tokenIterator = -1
    
    # Initializes the fileName and the token List
    def __init__(self,fileName):
        self.tokenList = []
        self.tokenIterator = -1
        self.fileName = fileName
        # TODO: fic this, coordinate with c sharp caller
        file = open("C:\SamTest\powershell\\"+self.fileName,'r')
        token = ""
        while 1:
            char = file.read(1)
            if not char:
                break
            if char == ';':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("SEMICOLON")
            elif char == '\n':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("NEWLINE")
            elif char == '\t':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("TABSPACE")
            elif char == ':':
                if token is not '':
                    self.tokenList.append(token)
                    token  = ""
                self.tokenList.append("COLON")
            elif char == ',':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("COMMA")
            elif char == '(':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("OPENBRACKET")
            elif char == ')':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("CLOSEBRACKET")
            elif char == '=':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("EQUAL")
            elif char == ' ':
                if token is not '':
                    self.tokenList.append(token)
                    token = ""
                self.tokenList.append("SPACE")
            else:
                token += char
        self.tokenList.append("EOF")
        #print(self.tokenList)
    
        file.close()
    
    def getNextPosition(self):
        self.tokenIterator += 1
        return self.tokenIterator
    
    def getnextToken(self):
        return self.tokenList[self.getNextPosition()]
    
    def getFileName(self):
        return self.fileName
    
    def getTokenList(self):
        return self.tokenList
    
    
