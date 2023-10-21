#ifndef INSTANCE_H
#define INSTANCE_H
#include "Element.h"
class Instance : public Element {
    std::string instanceType;
    std::string libraryItem; // change to Item object once implemented
public:
    Instance() = delete;
    Instance(pugi::xml_node& elementNode);
    Instance(std::string& instanceType, std::string& libraryItem);
    Instance(const Instance& instance);
    std::string getInstanceType() const;
    std::string getLibraryItem() const;
    void setLibraryItem(const std::string& libraryItem);
};
#endif // INSTANCE_H