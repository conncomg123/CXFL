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
	Element();
	Element(pugi::xml_node& elementNode, std::string elementType);
	virtual ~Element();
	Element(const Element& element);
	virtual double getWidth() const = 0;
	void setWidth(double width);
	virtual double getHeight() const = 0;
	void setHeight(double height);
	std::string getElementType();
	pugi::xml_node& getRoot();
};
const std::vector<std::string_view> ACCEPTABLE_ELEMENT_TYPES = {"shape", "text", "tflText", "instance", "shapeObj"}; 
#endif // ELEMENT_H