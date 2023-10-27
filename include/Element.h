#ifndef ELEMENT_H
#define ELEMENT_H
#define UNDEF -1.0
#include "pugixml.hpp"
#include <vector>

class Element {
protected:
	pugi::xml_node root;
	std::string elementType;
	double width, height;
public:
	Element() noexcept;
	Element(pugi::xml_node& elementNode, std::string elementType) noexcept(false);
	virtual ~Element() noexcept;
	Element(const Element& element) noexcept;
	virtual double getWidth() const = 0;
	void setWidth(double width) noexcept;
	virtual double getHeight() const = 0;
	void setHeight(double height) noexcept;
	const std::string& getElementType() const noexcept;
	pugi::xml_node& getRoot() noexcept;
	const pugi::xml_node& getRoot() const noexcept;
};
const std::vector<std::string_view> ACCEPTABLE_ELEMENT_TYPES = {"shape", "text", "tflText", "instance", "shapeObj"}; 
#endif // ELEMENT_H