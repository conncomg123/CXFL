#ifndef INSTANCE_H
#define INSTANCE_H
#include "Element.h"
class Instance : public Element {
    std::string instanceType;
    std::string libraryItem; // change to Item object once implemented
public:
    Instance() = delete;
    Instance(pugi::xml_node& elementNode) noexcept;
    Instance(std::string& instanceType, std::string& libraryItem) noexcept;
    Instance(const Instance& instance) noexcept;
    std::string getInstanceType() const noexcept;
    std::string getLibraryItem() const noexcept;
    void setLibraryItem(const std::string& libraryItem) noexcept;
};
#endif // INSTANCE_H