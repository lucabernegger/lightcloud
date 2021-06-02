local SplitterEingang = {
    splitter = component.proxy("AAA00CAE4D6AF16AF51895BD8E336E2F"),
    items = {{"Verpackter Schwerölrückstand", 1},{"Polymerharz", 1}},
    overflowPort = 2,
    lastPort = 0,
    itemCount = 0
}
local SplitterEingangZwo = {
    splitter = component.proxy("3BE35CD944B9EFD8C04B2EA35CA6AAF6"),
    items = {{"Abgefüllter Treibstoff", 0}, {"Caterium-Barren", 0}},
    overflowPort = 1,
    lastPort = 0,
    itemCount = 0
}
local SplitterSchredder = {
    splitter = component.proxy("28D079D44643FE68FA3DD2B2899FF351"),
    items = {{"Energiefragment", 2}, {"FICSIT-Coupon", 2}, {"S.A.M.-Erz", 2}, {"Alien-Organe", 2}, {"Alien-Panzer", 2}, {"Wheat", 2}, {"Potato", 2}, {"Holz", 2}, {"Blätter", 2}, {"Blütenblätter", 2}, {"Beryllnuss", 2}, {"Paläobeere", 2}, {"Steckpilz", 2}, {"Myzelium", 2}},
    overflowPort = 1,
    lastPort = 0,
    itemCount = 0
}
local FabricatorSplitter = {
    splitter = component.proxy("ED4F18E24817981F27A829BABF9884FD"),
    items = {{"Modularer Rahmen", 0}, {"Stahlrohr", 0}, {"Stahlbetonträger", 0}, {"Schraube", 0}},
    overflowPort = 1,
    lastPort = 0,
    itemCount = 0
}
local Fabricator3OGSplitter = {
    splitter = component.proxy("FB3A6C6A48F45FC60A41E2A99EE1572B"),
    items = {{"Verstärkte Eisenplatte", 2}, {"Rotor", 2}, {"Modularer Rahmen", 2}, {"Stahlträger", 2}, {"Stator", 2}, {"Kabel", 2}, {"Alclad-Aluminiumplatte", 2}, {"Gummi", 2},{"Schwerer modularer Rahmen", 2},{"Quarzkristall", 2}},
    overflowPort = 1,
    lastPort = 0,
    itemCount = 0
}
SplitterList = {SplitterEingang,SplitterSchredder,FabricatorSplitter,Fabricator3OGSplitter,SplitterEingangZwo}

local Eisenschmiede = {
    name = "Eisenschmiede\n",
    machines = {
        component.proxy("6DE78E964C75E4A3A32180AFDA8B4D17"),
        component.proxy("F0F6B85047F6EDB70BAF4BBAFDA0C761"),
        component.proxy("6EA2B75E4B951850F71BFCBF39E8C55B"),
        component.proxy("C8D9858B4A99FDD59B0C499895A8413D"),
        component.proxy("963FC05C4F2A392B89F8DAAF90CEE3B1"),
        component.proxy("92F95773437D65AF9FD1AB9903D6D500"),
        component.proxy("861546754234B38B900BB4864B4C4EBC"),
        component.proxy("87B89891400FFD20E0312691B2B916F2"),
        component.proxy("8E08946B4CE8F4674E732D97A0AC8734")
    },
    lever = component.proxy("A69FED19422D06C163C97884AB59E778"):getModule(4,8),
    display = component.proxy("A69FED19422D06C163C97884AB59E778"):getModule(0,8)
}

local Stahl = {
    name = "Stahlgiesserei\n",
    machines = {
        component.proxy("610276544A644FC8BED549953334E58D"),
        component.proxy("B1DCC72543CA112AE8F7C1B5FBC8E363"),
        component.proxy("11E55F9F4838EE9F528CB18077E47964")
    },
    lever = component.proxy("A69FED19422D06C163C97884AB59E778"):getModule(6,8),
    display = component.proxy("A69FED19422D06C163C97884AB59E778"):getModule(7,8)
}
FactoryGroupList = {Eisenschmiede,Stahl}

local reactorCasing = {
    machineComponent = component.proxy("846A65834D6D02C289B0539811B15EB8"),
    maxItemCount = 100,
    inventoryComponent = component.proxy("15708E734222AB2F144409ACEEFF33AB")
}
FactoryMaxItemList = {reactorCasing}
function FactoryMaxItem()
    for i = 1, #FactoryMaxItemList, 1 do
        local fobj = FactoryMaxItemList[i]
        local count = fobj.inventoryComponent:getInventories()[1].itemCount;
        print(count)
        if(count >= fobj.maxItemCount) then
            fobj.machineComponent.Standby = true
        else
            if fobj.machineComponent.Standby == true then
                fobj.machineComponent.Standby = false
            end
        end
        
    end
end
function SplitterTransfer(table)
    if(table.splitter:getInput() ~= nil) then table.itemCount = table.itemCount + 1 end
    if (table.splitter ~= nil) then
        local output = false
        for i = 0, 2 do
            if (i ~= table.overflowPort and table.splitter:canOutput(i)) then output = true end
        end
        
        
        if (output) then
            local item = table.splitter:getInput()
            if (item) then
                for i = 1, #table.items do
                    if (table.items[i][1] == tostring(item.type)) then
                        for j = 2, #table.items[i], 1 do
                            local value = table.items[i][j]
                            if (table.splitter:canOutput(value) and table.lastPort ~= value) then
                                table.splitter:transferItem(value)
                                table.lastPort = value
                                break
                            end
                        end
                    end
                end
                table.splitter:transferItem(table.overflowPort)
                table.lastPort = table.overflowPort
            end
        
        else
            table.splitter:transferItem(table.overflowPort)
            table.lastPort = table.overflowPort
        end
    end
end

function FactoryGroup(obj,param)
    for i = 1, #FactoryGroupList, 1 do
        local fobj = FactoryGroupList[i]
        if(fobj.lever == obj) then
            for _, m in ipairs(fobj.machines) do
                m.Standby = param
             end
             if(fobj.display ~= nil) then
                if(param == true)then
                    fobj.display:setText(fobj.name .. "Aus")
                else 
                    fobj.display:setText(fobj.name .. "An")
                end
             end
        end
    end
end

local panel = component.proxy("A69FED19422D06C163C97884AB59E778")
local screen1 = panel:getModule(10,10)
local screen2 = panel:getModule(0,10)

screen1:setSize(50)
screen2:setSize(50)


local masterPowerController = component.proxy("B0262AD24CF8D01B1D6B09ABDD3C24E1")
local masterPowerLever = panel:getModule(5,10)
event.listen(masterPowerLever)

for i = 1, #FactoryGroupList, 1 do
    local obj = FactoryGroupList[i];
    event.listen(obj.lever)
    if(obj.display ~= nil) then
        obj.display:setSize(50)
        if(obj.machines[1] ~= nil and obj.machines[i].Standby == true)then
            obj.display:setText(obj.name .. "Aus")
        else 
            obj.display:setText(obj.name .. "An")
        end
        
    end
end

while (true) do
    for i = 1, #SplitterList do
        SplitterTransfer(SplitterList[i])
    end
    screen1:setText("Schredder:\n"..SplitterSchredder.itemCount)
    screen2:setText("Eingang:\n"..SplitterEingang.itemCount)
	

    local signal,obj,param = event.pull(0)
    if obj == masterPowerLever then 
        masterPowerController:setConnected(param)
    end
    FactoryMaxItem()

    FactoryGroup(obj,param)
end
