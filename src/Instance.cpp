#include "../include/Instance.h"
Instance::Instance(pugi::xml_node& elementNode) : Element(elementNode, "instance") {
    this->instanceType = elementNode.name();
    this->libraryItem = elementNode.attribute("libraryItemName").as_string();
}
Instance::Instance(std::string& instanceType, std::string& libraryItem) {
    this->instanceType = instanceType;
    this->libraryItem = libraryItem;
}
Instance::Instance(const Instance& instance) : Element(instance) {
    this->instanceType = instance.getInstanceType();
    this->libraryItem = instance.getLibraryItem();
}
std::string Instance::getInstanceType() const {
    return this->instanceType;
}
std::string Instance::getLibraryItem() const {
    return this->libraryItem;
}
void Instance::setLibraryItem(const std::string& libraryItem) {
    if (this->Element::root.attribute("libraryItemName").empty()) this->Element::root.append_attribute("libraryItemName");
    this->Element::root.attribute("libraryItemName").set_value(libraryItem.c_str());
    this->libraryItem = libraryItem;
}