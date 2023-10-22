#include "../include/Instance.h"
Instance::Instance(pugi::xml_node& elementNode) noexcept : Element(elementNode, "instance")  {
    this->instanceType = elementNode.name();
    this->libraryItem = elementNode.attribute("libraryItemName").as_string();
}
Instance::Instance(std::string& instanceType, std::string& libraryItem) noexcept {
    this->instanceType = instanceType;
    this->libraryItem = libraryItem;
}
Instance::Instance(const Instance& instance) noexcept : Element(instance) {
    this->instanceType = instance.getInstanceType();
    this->libraryItem = instance.getLibraryItem();
}
std::string Instance::getInstanceType() const noexcept {
    return this->instanceType;
}
std::string Instance::getLibraryItem() const noexcept {
    return this->libraryItem;
}
void Instance::setLibraryItem(const std::string& libraryItem) noexcept {
    if (this->Element::root.attribute("libraryItemName").empty()) this->Element::root.append_attribute("libraryItemName");
    this->Element::root.attribute("libraryItemName").set_value(libraryItem.c_str());
    this->libraryItem = libraryItem;
}